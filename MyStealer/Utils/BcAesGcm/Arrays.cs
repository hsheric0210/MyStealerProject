using System.Runtime.CompilerServices;

namespace MyStealer.Utils.BcAesGcm
{
    /// <summary> General array utilities.</summary>
    public static class Arrays
    {
        public static readonly byte[] EmptyBytes = new byte[0];
        public static readonly int[] EmptyInts = new int[0];

        /// <summary>
        /// Are two arrays equal.
        /// </summary>
        /// <param name="a">Left side.</param>
        /// <param name="b">Right side.</param>
        /// <returns>True if equal.</returns>
        public static bool AreEqual(byte[] a, byte[] b)
        {
            if (a == b)
                return true;

            if (a == null || b == null)
                return false;

            return HaveSameContents(a, b);
        }

#if !(NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
#endif
        public static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (null == a || null == b)
                return false;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return CryptographicOperations.FixedTimeEquals(a, b);
#else
            var len = a.Length;
            if (len != b.Length)
                return false;

            var d = 0;
            for (var i = 0; i < len; ++i)
            {
                d |= a[i] ^ b[i];
            }
            return 0 == d;
#endif
        }

        private static bool HaveSameContents(
            byte[] a,
            byte[] b)
        {
            var i = a.Length;
            if (i != b.Length)
                return false;
            while (i != 0)
            {
                --i;
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }

        public static byte[] Clone(byte[] data) => data == null ? null : (byte[])data.Clone();

        public static void Fill(byte[] buf, byte b)
        {
            var i = buf.Length;
            while (i > 0)
            {
                buf[--i] = b;
            }
        }

        internal static void Reverse<T>(T[] input, T[] output)
        {
            var last = input.Length - 1;
            for (var i = 0; i <= last; ++i)
            {
                output[i] = input[last - i];
            }
        }
    }
}
