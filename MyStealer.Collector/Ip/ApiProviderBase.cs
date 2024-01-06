using System;

namespace MyStealer.Collector.Ip
{
    public abstract class ApiProviderBase : ModuleBase
    {
        public abstract Uri ApiUrl { get; }

        public override bool IsAvailable() => true;

        public abstract IpDetails Parse(string response);
    }
}
