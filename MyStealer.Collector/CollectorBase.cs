using MyStealer.Shared;
using System;

namespace MyStealer.Collector
{
    /// <summary>
    /// Base of all 'Collector' modules.
    /// </summary>
    public abstract class CollectorBase : ModuleBase, IDisposable
    {
        /// <summary>
        /// Return if this module is available on the current PC.
        /// </summary>
        /// <returns></returns>
        public abstract bool IsAvailable();

        /// <summary>
        /// Initialize the module.
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Unload all resources associated with this module.
        /// </summary>
        public virtual void Dispose() { }
    }
}
