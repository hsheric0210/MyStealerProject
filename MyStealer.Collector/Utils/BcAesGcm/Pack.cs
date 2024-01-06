#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
using System.Buffers.Binary;
#endif


namespace MyStealer.Collector.Utils.BcAesGcm
{
    public static class Pack
    {
        public static void UInt32_To_BE(uint n, byte[] bs, int off)
        {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            BinaryPrimitives.WriteUInt32BigEndian(bs.AsSpan(off), n);
#else
            bs[off] = (byte)(n >> 24);
            bs[off + 1] = (byte)(n >> 16);
            bs[off + 2] = (byte)(n >> 8);
            bs[off + 3] = (byte)n;
#endif
        }

        public static uint BE_To_UInt32(byte[] bs, int off)
        {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return BinaryPrimitives.ReadUInt32BigEndian(bs.AsSpan(off));
#else
            return (uint)bs[off] << 24
                | (uint)bs[off + 1] << 16
                | (uint)bs[off + 2] << 8
                | bs[off + 3];
#endif
        }

        public static void UInt64_To_BE(ulong n, byte[] bs, int off)
        {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            BinaryPrimitives.WriteUInt64BigEndian(bs.AsSpan(off), n);
#else
            UInt32_To_BE((uint)(n >> 32), bs, off);
            UInt32_To_BE((uint)n, bs, off + 4);
#endif
        }

        public static ulong BE_To_UInt64(byte[] bs, int off)
        {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return BinaryPrimitives.ReadUInt64BigEndian(bs.AsSpan(off));
#else
            var hi = BE_To_UInt32(bs, off);
            var lo = BE_To_UInt32(bs, off + 4);
            return (ulong)hi << 32 | lo;
#endif
        }

        public static void UInt32_To_LE(uint n, byte[] bs, int off)
        {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            BinaryPrimitives.WriteUInt32LittleEndian(bs.AsSpan(off), n);
#else
            bs[off] = (byte)n;
            bs[off + 1] = (byte)(n >> 8);
            bs[off + 2] = (byte)(n >> 16);
            bs[off + 3] = (byte)(n >> 24);
#endif
        }

        public static uint LE_To_UInt32(byte[] bs, int off)
        {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return BinaryPrimitives.ReadUInt32LittleEndian(bs.AsSpan(off));
#else
            return bs[off]
                | (uint)bs[off + 1] << 8
                | (uint)bs[off + 2] << 16
                | (uint)bs[off + 3] << 24;
#endif
        }
    }
}
