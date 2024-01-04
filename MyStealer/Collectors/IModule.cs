using System;

namespace MyStealer.Collectors
{
    internal interface IModule : IDisposable
    {
        string ApplicationName { get; }
        bool IsAvailable();
        void Initialize();
    }
}
