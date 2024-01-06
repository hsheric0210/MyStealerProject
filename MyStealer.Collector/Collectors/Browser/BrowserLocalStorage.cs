using System;

namespace MyStealer.Collectors.Browser
{
    public struct BrowserLocalStorage
    {
        public string BrowserName { get; set; }
        public string BrowserProfileName { get; set; }
        public string Host { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime AccessTimeStamp { get; set; }

        public override string ToString() => $"{nameof(BrowserLocalStorage)}({BrowserName} - {BrowserProfileName}){{{Host} -> {Key} = {Value}}}";
    }
}
