using System.Net;

namespace MyStealer.Collectors.Ip
{
    public struct IpDetails
    {
        public static readonly IpDetails Empty = new IpDetails()
        {
            Address = IPAddress.None,
            CountryCode = "",
            RegionName = "",
            City = ""
        };

        public IPAddress Address { get; set; }
        public string CountryCode { get; set; }
        public string RegionName { get; set; }
        public string City { get; set; }
    }
}
