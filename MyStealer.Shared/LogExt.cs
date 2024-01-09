
namespace MyStealer.Shared
{
    /// <summary>
    /// Logger abstraction layer factory.
    /// </summary>
    public static class LogExt
    {
        public static ILogger BaseLogger { get; set; }

        public static ILogger ForModule(string moduleName) => BaseLogger.ForContext("Module", moduleName);
    }
}
