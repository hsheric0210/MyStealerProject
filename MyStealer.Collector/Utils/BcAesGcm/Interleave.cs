namespace MyStealer.Collector.Utils.BcAesGcm
{
    public static class Interleave
    {
        private const ulong M64R = 0xAAAAAAAAAAAAAAAAUL;

        public static ulong Expand64To128Rev(ulong x, out ulong low)
        {
#if NETCOREAPP3_0_OR_GREATER
            if (Bmi2.X64.IsSupported)
            {
                low  = Bmi2.X64.ParallelBitDeposit(x >> 32, 0xAAAAAAAAAAAAAAAAUL);
                return Bmi2.X64.ParallelBitDeposit(x      , 0xAAAAAAAAAAAAAAAAUL);
            }
#endif

            // "shuffle" low half to even bits and high half to odd bits
            x = Bits.BitPermuteStep(x, 0x00000000FFFF0000UL, 16);
            x = Bits.BitPermuteStep(x, 0x0000FF000000FF00UL, 8);
            x = Bits.BitPermuteStep(x, 0x00F000F000F000F0UL, 4);
            x = Bits.BitPermuteStep(x, 0x0C0C0C0C0C0C0C0CUL, 2);
            x = Bits.BitPermuteStep(x, 0x2222222222222222UL, 1);

            low = x & M64R;
            return x << 1 & M64R;
        }
    }
}
