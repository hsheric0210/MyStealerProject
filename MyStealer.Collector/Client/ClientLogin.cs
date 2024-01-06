namespace MyStealer.Collector.Client
{
    public struct ClientLogin
    {
        public string ProgramName { get; set; }
        public string Name { get; set; }
        public LoginProtocol Protocol { get; set; }
        public string Host { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public override string ToString() => $"{nameof(ClientLogin)}({ProgramName} - {Name}: {Host} -> {UserName}:{Password})";
    }

    public enum LoginProtocol
    {
        None = 0,
        FTP, // File Transfer Protocol
        SSH, // SSH
        SCP,
        WebDAV,
        AmazonS3,
    }
}
