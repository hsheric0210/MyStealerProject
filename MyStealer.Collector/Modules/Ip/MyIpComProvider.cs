using Newtonsoft.Json.Linq;
using System;
using System.Net;

namespace MyStealer.Collector.Modules.Ip
{
    /// <summary>
    /// https://www.myip.com/api-docs/
    /// </summary>
    public class MyIpComProvider : ApiProviderBase
    {
        public override string ModuleName => "www.myip.com";

        public override Uri ApiUrl => new Uri("https://api.myip.com");

        public override IpDetails Parse(string response)
        {
            try
            {
                var obj = JObject.Parse(response);

                return new IpDetails
                {
                    Address = IPAddress.Parse(obj["ip"].Value<string>()),
                    CountryCode = obj["cc"].Value<string>(),
                    RegionName = "",
                    City = ""
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
