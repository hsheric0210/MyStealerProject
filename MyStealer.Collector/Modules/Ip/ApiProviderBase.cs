using MyStealer.Collector.Modules;
using System;

namespace MyStealer.Collector.Modules.Ip
{
    public abstract class ApiProviderBase : ModuleBase
    {
        public abstract Uri ApiUrl { get; }

        public override bool IsAvailable() => true;

        public abstract IpDetails Parse(string response);
    }
}
