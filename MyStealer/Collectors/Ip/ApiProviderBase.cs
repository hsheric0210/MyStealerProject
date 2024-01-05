using System;

namespace MyStealer.Collectors.Ip
{
    internal abstract class ApiProviderBase : ModuleBase
    {
        public abstract Uri ApiUrl { get; }

        public override bool IsAvailable() => true;

        public abstract IpDetails Parse(string response);
    }
}
