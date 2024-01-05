using System.Collections.Immutable;

namespace MyStealer.Collectors.Game
{
    internal abstract class GameCollector : ModuleBase
    {
        public abstract IImmutableSet<GameLogin> GetLogins();
    }
}
