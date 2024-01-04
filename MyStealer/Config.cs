namespace MyStealer
{
    /// <summary>
    /// You can freely edit these config values even after compiling the program, using MyStealerToolbox.
    /// 
    /// </summary>
    internal static class Config
    {
        public static readonly string EncryptionKey = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghkijklmnopqrstuvwxyz0123456789";
        public static readonly string LogFilePath = "log.log";
        public static readonly string ReverseConnectIpAndPort = "0.0.0.0:1337";
    }
}
