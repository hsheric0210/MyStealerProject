using System.Collections.Generic;

namespace MyStealer.Collectors.Browser
{
    internal interface IBrowserCollector : IModule
    {
        ISet<CredentialEntry> GetCredentials();
        ISet<CookieEntry> GetCookies();
        ISet<LocalStorageEntry> GetLocalStorageEntries();
    }
}
