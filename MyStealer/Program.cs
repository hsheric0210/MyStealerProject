using Serilog;

namespace MyStealer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.File(Config.LogFilePath, hooks: new LogEncryptionHook()).CreateLogger();
        }
    }
}
