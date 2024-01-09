using MyStealer.Collector.Properties;
using System;
using System.IO;

namespace MyStealer.Collector
{
    public static class NativeMethods
    {
        public const string SqliteInterop = "SQLite.Interop.dll";

        /// <summary>
        /// Load the correct SQLite native library depending on the current PC architecture (32 or 64 bit)
        /// </summary>
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
