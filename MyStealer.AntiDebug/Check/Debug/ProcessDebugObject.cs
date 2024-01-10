using System;
using System.Diagnostics;
using static MyStealer.AntiDebug.NativeCalls;

namespace MyStealer.AntiDebug.Check.Debug
{
    /// <summary>
    /// https://github.com/AdvDebug/AntiCrack-DotNet/blob/91872f71c5601e4b037b713f31327dfde1662481/AntiCrack-DotNet/AntiDebug.cs
    /// </summary>
    public class ProcessDebugObject : CheckBase
    {
        public override string Name => "ProcessInformation DebugObject";

        public override bool CheckActive()
        {
            var size = (uint)(sizeof(uint) * (Environment.Is64BitProcess ? 2 : 1));
            NtQueryInformationProcess_IntPtr(Process.GetCurrentProcess().SafeHandle, 0x1e, out IntPtr dbgObject, size, 0);
            return dbgObject != IntPtr.Zero;
        }
    }
}
