using System.Diagnostics;
using static MyStealer.AntiDebug.NativeCalls;

namespace MyStealer.AntiDebug.Check.Debug
{
    /// <summary>
    /// https://github.com/AdvDebug/AntiCrack-DotNet/blob/91872f71c5601e4b037b713f31327dfde1662481/AntiCrack-DotNet/AntiDebug.cs
    /// </summary>
    public class ProcessDebugFlags : CheckBase
    {
        public override string Name => "ProcessInformation DebugFlags";

        public override bool CheckActive()
        {
            NtQueryInformationProcess_uint(Process.GetCurrentProcess().SafeHandle, 0x1f, out var flag, sizeof(uint), 0);
            return flag == 0;
        }
    }
}
