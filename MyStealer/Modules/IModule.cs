using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyStealer.Modules
{
    internal interface IModule
    {
        string ApplicationName { get; }
        bool Check();
        void Initialize();
    }
}
