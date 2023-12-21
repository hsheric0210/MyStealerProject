using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyStealer.Collectors
{
    internal interface IModule
    {
        string ApplicationName { get; }
        bool IsAvailable();
        void Initialize();
    }
}
