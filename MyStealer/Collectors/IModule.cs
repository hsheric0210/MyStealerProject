namespace MyStealer.Collectors
{
    internal interface IModule
    {
        string ApplicationName { get; }
        bool IsAvailable();
        void Initialize();
    }
}
