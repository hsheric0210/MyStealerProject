using System;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
using System.Buffers;
#endif

namespace MyStealer.Utils.BcAesGcm
{
    public class ParametersWithIV
        : ICipherParameters
    {
        public static ICipherParameters ApplyOptionalIV(ICipherParameters parameters, byte[] iv) => iv == null ? parameters : new ParametersWithIV(parameters, iv);

        private readonly ICipherParameters m_parameters;
        private readonly byte[] m_iv;

        public ParametersWithIV(ICipherParameters parameters, byte[] iv)
            : this(parameters, iv, 0, iv.Length)
        {
            // NOTE: 'parameters' may be null to imply key re-use
            if (iv == null)
                throw new ArgumentNullException(nameof(iv));

            m_parameters = parameters;
            m_iv = (byte[])iv.Clone();
        }

        public ParametersWithIV(ICipherParameters parameters, byte[] iv, int ivOff, int ivLen)
        {
            // NOTE: 'parameters' may be null to imply key re-use
            if (iv == null)
                throw new ArgumentNullException(nameof(iv));

            m_parameters = parameters;
            m_iv = new byte[ivLen];
            Array.Copy(iv, ivOff, m_iv, 0, ivLen);
        }

        private ParametersWithIV(ICipherParameters parameters, int ivLength)
        {
            if (ivLength < 0)
                throw new ArgumentOutOfRangeException(nameof(ivLength));

            // NOTE: 'parameters' may be null to imply key re-use
            m_parameters = parameters;
            m_iv = new byte[ivLength];
        }

        public void CopyIVTo(byte[] buf, int off, int len)
        {
            if (m_iv.Length != len)
                throw new ArgumentOutOfRangeException(nameof(len));

            Array.Copy(m_iv, 0, buf, off, len);
        }

        public byte[] GetIV() => (byte[])m_iv.Clone();

        public int IVLength => m_iv.Length;

        public ICipherParameters Parameters => m_parameters;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public ReadOnlySpan<byte> IV => m_iv;
#endif
    }
}
