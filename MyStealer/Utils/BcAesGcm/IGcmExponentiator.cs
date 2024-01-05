using System;

namespace MyStealer.Utils.BcAesGcm
{
    [Obsolete("Will be removed")]
    public interface IGcmExponentiator
    {
        void Init(byte[] x);
        void ExponentiateX(long pow, byte[] output);
    }
}
