using Serilog;
using System;
using System.Globalization;
using System.Linq;

namespace MyStealer.Handler.Chromium.LevelDb
{
    public static class BytesExtension
    {
        public static byte[] ToLittleEndian(this byte[] data)
        {
            if (BitConverter.IsLittleEndian)
                return data;

            var buffer = new byte[data.Length];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            Array.Reverse(buffer); // if it's big-endian, reverse.
            return buffer;
        }
        public static byte[] ToBigEndian(this byte[] data)
        {
            if (!BitConverter.IsLittleEndian)
                return data;

            var buffer = new byte[data.Length];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            Array.Reverse(buffer);
            return buffer;
        }

        public static ulong ToLeUInt64(this byte[] data, int startIndex = 0) => BitConverter.ToUInt64(data.ToLittleEndian(), startIndex);

        public static uint ToLeUInt32(this byte[] data, int startIndex = 0) => BitConverter.ToUInt32(data.ToLittleEndian(), startIndex);

        public static ushort ToLeUInt16(this byte[] data, int startIndex = 0) => BitConverter.ToUInt16(data.ToLittleEndian(), startIndex);

        public static long ToLeInt64(this byte[] data, int startIndex = 0) => BitConverter.ToInt64(data.ToLittleEndian(), startIndex);

        public static int ToLeInt32(this byte[] data, int startIndex = 0) => BitConverter.ToInt32(data.ToLittleEndian(), startIndex);

        public static short ToLeInt16(this byte[] data, int startIndex = 0) => BitConverter.ToInt16(data.ToLittleEndian(), startIndex);

        public static byte[] Append(this byte[] data, byte[] more)
        {
            var buffer = new byte[data.Length + more.Length];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            Buffer.BlockCopy(more, 0, buffer, data.Length, more.Length);
            return buffer;
        }

        public static bool StartsWith(this byte[] data, byte[] target)
        {
            if (target.Length > data.Length)
                return false;

            for (int i = 0, j = target.Length; i < j; i++)
            {
                if (data[i] != target[i])
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
            Buffer.BlockCopy(data, idx+1, second, 0, second.Length);
            return (first, second);
        }
    }
}
