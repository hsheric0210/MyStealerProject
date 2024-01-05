using System;

namespace MyStealer.Utils.BcAesGcm
{
    [Obsolete("Will be removed")]
    public interface IGcmMultiplier
    {
        void Init(byte[] H);
        void MultiplyH(byte[] x);
    }
}
