using MyStealer.AntiDebug.Utils;
using System;
using System.Runtime.InteropServices;
using static MyStealer.AntiDebug.NativeCalls;

namespace MyStealer.AntiDebug.Check.Debugging
{
    /// <summary>
    /// https://github.com/AdvDebug/AntiCrack-DotNet/blob/91872f71c5601e4b037b713f31327dfde1662481/AntiCrack-DotNet/AntiDebug.cs
    /// </summary>
    public class OutputDebugString : CheckBase
    {
        public override string Name => "OutputDebugString";

        public override bool CheckActive()
        {
            var random = new Random();
            OutputDebugStringA(StringUtils.RandomString(random.Next(512), random));
            return Marshal.GetLastWin32Error() == 0;
        }
    }
}
