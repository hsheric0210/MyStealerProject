using System.Diagnostics;

namespace MyStealer.AntiDebug.Check.Debug
{
    /// <summary>
    /// https://github.com/AdvDebug/AntiCrack-DotNet/blob/91872f71c5601e4b037b713f31327dfde1662481/AntiCrack-DotNet/AntiDebug.cs
    /// </summary>
    public class DebuggerBreak : CheckBase
    {
        public override string Name => "Debugger.Break";

        public override bool CheckActive()
        {
            try
            {
                Debugger.Break();
                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}
