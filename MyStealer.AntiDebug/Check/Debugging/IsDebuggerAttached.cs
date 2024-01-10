using System.Diagnostics;

namespace MyStealer.AntiDebug.Check.Debugging
{
    /// <summary>
    /// https://github.com/AdvDebug/AntiCrack-DotNet/blob/91872f71c5601e4b037b713f31327dfde1662481/AntiCrack-DotNet/AntiDebug.cs
    /// </summary>
    public class IsDebuggerAttached : CheckBase
    {
        public override string Name => "IsDebuggerAttached";

        public override bool CheckActive() => Debugger.IsAttached;
    }
}
