using MyStealer.Collector.Properties;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MyStealer.Collector
{
    public static class NativeMethods
    {
        public const string SqliteInterop = "SQLite.Interop.dll";

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        public static void WriteLibraries()
        {
            var is64 = Environment.Is64BitProcess;
            var data = is64 ? Resources.SQLiteInterop64 : Resources.SQLiteInterop32;
            File.WriteAllBytes(SqliteInterop, data);
        }

        public static void CleanupLibraries()
        {
            var file = new FileInfo(SqliteInterop);
            if (file.Exists)
                file.Delete();
        }
    }
}
