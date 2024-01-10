using static MyStealer.AntiDebug.NativeCalls;

namespace MyStealer.AntiDebug.Check.Debugging
{
    /// <summary>
    /// https://github.com/AdvDebug/AntiCrack-DotNet/blob/91872f71c5601e4b037b713f31327dfde1662481/AntiCrack-DotNet/AntiDebug.cs
    /// </summary>
    public class IsDebuggerPresent : CheckBase
    {
        public override string Name => "IsDebuggerPresent";

        public override bool CheckActive() => IsDebuggerPresent();
    }
}
