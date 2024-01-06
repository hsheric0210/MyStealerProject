
namespace MyStealer.Shared
{
    public static class LogExt
    {
        public static ILogger BaseLogger { get; set; }

        public static ILogger ForModule(string moduleName) => BaseLogger.ForContext("Module", moduleName);
    }
}
