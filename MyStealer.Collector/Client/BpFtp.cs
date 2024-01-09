using System;
using System.Collections.Immutable;

namespace MyStealer.Collector.Client
{
    /// <summary>
    /// Ported from Quasar RAT
    /// https://github.com/quasar/Quasar/blob/master/Quasar.Client/Recovery/FtpClients/WinScpPassReader.cs
    /// </summary>
    public class BpFtp : ClientCollector
    {
        public override string Name => "BulletProof FTP Client";

        public override bool IsAvailable() => throw new NotImplementedException();

        public override IImmutableSet<ClientLogin> GetLogins() => throw new NotImplementedException();
    }
}
