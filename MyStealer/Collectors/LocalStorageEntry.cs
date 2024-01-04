using System;

namespace MyStealer.Collectors
{
    internal struct LocalStorageEntry
    {
        public string ApplicationName { get; set; }
        public string ApplicationProfileName { get; set; }
        public string Host { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime AccessTimeStamp { get; set; }

        public override string ToString() => $"Storage({ApplicationName} - {ApplicationProfileName}){{{Host} -> {Key} = {Value}}}";
    }
}
