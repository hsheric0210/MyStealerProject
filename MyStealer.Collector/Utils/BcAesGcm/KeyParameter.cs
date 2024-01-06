using System;

namespace MyStealer.Collector.Utils.BcAesGcm
{
    public class KeyParameter
        : ICipherParameters
    {

        private readonly byte[] m_key;

        public KeyParameter(byte[] key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            m_key = (byte[])key.Clone();
        }

        public KeyParameter(byte[] key, int keyOff, int keyLen)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (keyOff < 0 || keyOff > key.Length)
                throw new ArgumentOutOfRangeException(nameof(keyOff));
            if (keyLen < 0 || keyLen > key.Length - keyOff)
                throw new ArgumentOutOfRangeException(nameof(keyLen));

            m_key = new byte[keyLen];
            Array.Copy(key, keyOff, m_key, 0, keyLen);
        }

        private KeyParameter(int length)
        {
            if (length < 1)
                throw new ArgumentOutOfRangeException(nameof(length));

            m_key = new byte[length];
        }

        public void CopyTo(byte[] buf, int off, int len)
        {
            if (m_key.Length != len)
                throw new ArgumentOutOfRangeException(nameof(len));

            Array.Copy(m_key, 0, buf, off, len);
        }

        public byte[] GetKey() => (byte[])m_key.Clone();

        public int KeyLength => m_key.Length;

        public bool FixedTimeEquals(byte[] data) => Arrays.FixedTimeEquals(m_key, data);

        public KeyParameter Reverse()
        {
            var reversed = new KeyParameter(m_key.Length);
            Arrays.Reverse(m_key, reversed.m_key);
            return reversed;
        }
    }
}
