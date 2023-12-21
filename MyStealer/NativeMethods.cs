using MyStealer.Properties;
using Serilog;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MyStealer
{
    public static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        internal static void WriteSqliteInterop()
        {
            var is64 = Environment.Is64BitProcess;
            var interop = is64 ? Resources.SQLiteInterop64 : Resources.SQLiteInterop86;
            File.WriteAllBytes("SQLite.Interop.dll", interop); // The name is fixed; cannot be changed
            Log.Debug("Using {bit}-bit SQLite interop library", is64 ? 64 : 32);
        }

        internal static void CleanupSqliteInterop()
        {
            var file = new FileInfo("SQLite.Interop.dll");
            if (file.Exists)
            {
                Log.Debug("Deleted SQLite interop library");
                file.Delete();
            }
        }
    }
}
