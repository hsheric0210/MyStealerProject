using System.Diagnostics;

namespace MyStealer.Collector.Utils.BcAesGcm
{
    public static class GcmUtilities
    {
        public struct FieldElement
        {
            public ulong n0, n1;
        }

        private const uint E1 = 0xe1000000;
        private const ulong E1UL = (ulong)E1 << 32;

        public static void One(out FieldElement x)
        {
            x.n0 = 1UL << 63;
            x.n1 = 0UL;
        }

        public static void AsBytes(ulong x0, ulong x1, byte[] z)
        {
            Pack.UInt64_To_BE(x0, z, 0);
            Pack.UInt64_To_BE(x1, z, 8);
        }

        public static void AsBytes(ref FieldElement x, byte[] z) => AsBytes(x.n0, x.n1, z);

        public static void AsFieldElement(byte[] x, out FieldElement z)
        {
            z.n0 = Pack.BE_To_UInt64(x, 0);
            z.n1 = Pack.BE_To_UInt64(x, 8);
        }

        public static void DivideP(ref FieldElement x, out FieldElement z)
        {
            ulong x0 = x.n0, x1 = x.n1;
            var m = (ulong)((long)x0 >> 63);
            x0 ^= m & E1UL;
            z.n0 = x0 << 1 | x1 >> 63;
            z.n1 = x1 << 1 | (ulong)-(long)m;
        }

        public static void Multiply(byte[] x, byte[] y)
        {
            AsFieldElement(x, out var X);
            AsFieldElement(y, out var Y);
            Multiply(ref X, ref Y);
            AsBytes(ref X, x);
        }

        public static void Multiply(ref FieldElement x, ref FieldElement y)
        {
            ulong z0, z1, z2;

            /*
             * "Three-way recursion" as described in "Batch binary Edwards", Daniel J. Bernstein.
             *
             * Without access to the high part of a 64x64 product x * y, we use a bit reversal to calculate it:
             *     rev(x) * rev(y) == rev((x * y) << 1) 
             */

            ulong x0 = x.n0, x1 = x.n1;
            ulong y0 = y.n0, y1 = y.n1;
            ulong x0r = Longs.Reverse(x0), x1r = Longs.Reverse(x1);
            ulong y0r = Longs.Reverse(y0), y1r = Longs.Reverse(y1);
            ulong z3;

            var h0 = Longs.Reverse(ImplMul64(x0r, y0r));
            var h1 = ImplMul64(x0, y0) << 1;
            var h2 = Longs.Reverse(ImplMul64(x1r, y1r));
            var h3 = ImplMul64(x1, y1) << 1;
            var h4 = Longs.Reverse(ImplMul64(x0r ^ x1r, y0r ^ y1r));
            var h5 = ImplMul64(x0 ^ x1, y0 ^ y1) << 1;

            z0 = h0;
            z1 = h1 ^ h0 ^ h2 ^ h4;
            z2 = h2 ^ h1 ^ h3 ^ h5;
            z3 = h3;

            Debug.Assert(z3 << 63 == 0);

            z1 ^= z3 ^ z3 >> 1 ^ z3 >> 2 ^ z3 >> 7;
            //              z2 ^=      (z3 << 63) ^ (z3 << 62) ^ (z3 << 57);
            z2 ^= z3 << 62 ^ z3 << 57;

            z0 ^= z2 ^ z2 >> 1 ^ z2 >> 2 ^ z2 >> 7;
            z1 ^= z2 << 63 ^ z2 << 62 ^ z2 << 57;

            x.n0 = z0;
            x.n1 = z1;
        }

        public static void MultiplyP7(ref FieldElement x)
        {
            ulong x0 = x.n0, x1 = x.n1;
            var c = x1 << 57;
            x.n0 = x0 >> 7 ^ c ^ c >> 1 ^ c >> 2 ^ c >> 7;
            x.n1 = x1 >> 7 | x0 << 57;
        }

        public static void MultiplyP8(ref FieldElement x, out FieldElement y)
        {
            ulong x0 = x.n0, x1 = x.n1;
            var c = x1 << 56;
            y.n0 = x0 >> 8 ^ c ^ c >> 1 ^ c >> 2 ^ c >> 7;
            y.n1 = x1 >> 8 | x0 << 56;
        }

        public static void Square(ref FieldElement x)
        {
            var t1 = Interleave.Expand64To128Rev(x.n0, out var t0);
            var t3 = Interleave.Expand64To128Rev(x.n1, out var t2);

            Debug.Assert((t0 | t1 | t2 | t3) << 63 == 0UL);

            var z1 = t1 ^ t3 ^ t3 >> 1 ^ t3 >> 2 ^ t3 >> 7;
            var z2 = t2 ^ t3 << 62 ^ t3 << 57;

            x.n0 = t0 ^ z2 ^ z2 >> 1 ^ z2 >> 2 ^ z2 >> 7;
            x.n1 = z1 ^ t2 << 62 ^ t2 << 57;
        }

        public static void Xor(byte[] x, byte[] y)
        {
            var i = 0;
            do
            {
                x[i] ^= y[i];
                ++i;
                x[i] ^= y[i];
                ++i;
                x[i] ^= y[i];
                ++i;
                x[i] ^= y[i];
                ++i;
            }
            while (i < 16);
        }

        public static void Xor(byte[] x, byte[] y, int yOff)
        {
            var i = 0;
            do
            {
                x[i] ^= y[yOff + i];
                ++i;
                x[i] ^= y[yOff + i];
                ++i;
                x[i] ^= y[yOff + i];
                ++i;
                x[i] ^= y[yOff + i];
                ++i;
            }
            while (i < 16);
        }

        public static void Xor(byte[] x, byte[] y, int yOff, int yLen)
        {
            while (--yLen >= 0)
            {
                x[yLen] ^= y[yOff + yLen];
            }
        }

        public static void Xor(byte[] x, int xOff, byte[] y, int yOff, int len)
        {
            while (--len >= 0)
            {
                x[xOff + len] ^= y[yOff + len];
            }
        }

        public static void Xor(ref FieldElement x, ref FieldElement y, out FieldElement z)
        {
            z.n0 = x.n0 ^ y.n0;
            z.n1 = x.n1 ^ y.n1;
        }

        private static ulong ImplMul64(ulong x, ulong y)
        {
            var x0 = x & 0x1111111111111111UL;
            var x1 = x & 0x2222222222222222UL;
            var x2 = x & 0x4444444444444444UL;
            var x3 = x & 0x8888888888888888UL;

            var y0 = y & 0x1111111111111111UL;
            var y1 = y & 0x2222222222222222UL;
            var y2 = y & 0x4444444444444444UL;
            var y3 = y & 0x8888888888888888UL;

            var z0 = x0 * y0 ^ x1 * y3 ^ x2 * y2 ^ x3 * y1;
            var z1 = x0 * y1 ^ x1 * y0 ^ x2 * y3 ^ x3 * y2;
            var z2 = x0 * y2 ^ x1 * y1 ^ x2 * y0 ^ x3 * y3;
            var z3 = x0 * y3 ^ x1 * y2 ^ x2 * y1 ^ x3 * y0;

            z0 &= 0x1111111111111111UL;
            z1 &= 0x2222222222222222UL;
            z2 &= 0x4444444444444444UL;
            z3 &= 0x8888888888888888UL;

            return z0 | z1 | z2 | z3;
        }
    }
}
