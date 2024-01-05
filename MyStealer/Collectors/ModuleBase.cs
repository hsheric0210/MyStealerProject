using Serilog;
using System;

namespace MyStealer.Collectors
{
    internal abstract class ModuleBase : IDisposable
    {
        public abstract string ModuleName { get; }

        private ILogger lazyLogger;
        protected ILogger Logger => lazyLogger ?? (lazyLogger = LogExt.ForModule(ModuleName));

        public abstract bool IsAvailable();

        public virtual void Initialize() { }

        public virtual void Dispose() { }
    }
}
