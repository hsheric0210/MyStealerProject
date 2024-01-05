using System;

namespace MyStealer.Collectors.Browser
{
    internal struct BrowserCookie
    {
        public string BrowserName { get; set; }
        public string BrowserProfileName { get; set; }
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

        public override string ToString() => $"{nameof(BrowserCookie)}({BrowserName} - {BrowserProfileName}){{{Host} : {Path} -> {Name} = {Value}}}";
    }
}
