using System.Collections.Immutable;

namespace MyStealer.Collector.Client
{
    public abstract class ClientCollector : ModuleBase
    {
        public abstract IImmutableSet<ClientLogin> GetLogins();
    }
}
