using System;
using System.Collections.Generic;
using System.IO;
using MyStealer.Utils.Chromium.LevelDb;

namespace MyStealer.Utils
{
    public static class StreamExtension
    {
        /// <summary>
        /// Read Little-endian 64-bit signed integer from the stream
        /// </summary>
        /// <param name="stream">The stream to read data from</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static ulong ReadLeUInt64(this Stream stream)
            => stream.ReadBytesThen(8, bytes => bytes.ToLeUInt64(), true);

        public static uint ReadLeUInt32(this Stream stream)
            => stream.ReadBytesThen(4, bytes => bytes.ToLeUInt32(), true);

        public static ushort ReadLeUInt16(this Stream stream)
            => stream.ReadBytesThen(2, bytes => bytes.ToLeUInt16(), true);

        public static long ReadLeInt64(this Stream stream)
            => stream.ReadBytesThen(8, bytes => bytes.ToLeInt64(), true);

        public static int ReadLeInt32(this Stream stream)
            => stream.ReadBytesThen(4, bytes => bytes.ToLeInt32(), true);

        public static short ReadLeInt16(this Stream stream)
            => stream.ReadBytesThen(2, bytes => bytes.ToLeInt16(), true);

        public static byte[] ReadBytes(this Stream stream, int length, bool strict = false)
        {
            var buffer = new byte[length];
            var read = stream.Read(buffer, 0, length);
            if (strict && read != length)
                throw BufferUnderrunException.StreamUnderrun(length, read);

            return buffer;
        }

        public static T ReadBytesThen<T>(this Stream stream, int length, Func<byte[], T> callback, bool strict = false)
        {
            T result;
            var buffer = BytePool.Alloc(length);
            try
            {
                var read = stream.Read(buffer, 0, length);
                if (strict && read != length)
                    throw BufferUnderrunException.StreamUnderrun(length, read);

                result = callback(buffer);
            }
            finally
            {
                BytePool.Free(buffer);
            }
            return result;
        }

        private static (int, byte[]) InternalReadVarInt(Stream stream, bool is_google_32bit = false)
        {
            // this only outputs unsigned
            var i = 0;
            var result = 0;
            var limit = is_google_32bit ? 5 : 10;
            var underlying_bytes = new List<byte>(limit);
            while (i < limit)
            {
                var raw = stream.ReadByte();
                if (raw < 0)
                    return (0, Array.Empty<byte>());
                underlying_bytes.Add((byte)raw);
                result |= (raw & 0x7f) << i * 7;
                if ((raw & 0x80) == 0)
                    break;
                i++;
            }
            return (result, underlying_bytes.ToArray());
        }

        // Convenience version of _read_le_varint that only returns the value or None (if None, return 0)
        public static int ReadVarInt(this Stream stream, bool is_google_32bit = false)
        {
            var x = InternalReadVarInt(stream, is_google_32bit: is_google_32bit);
            return x.Item1;
        }

        // Reads a blob of data which is prefixed with a varint length
        public static byte[] ReadPrefixedBlob(this Stream stream)
        {
            var length = stream.ReadVarInt();
            var buffer = new byte[length];
            var read = stream.Read(buffer, 0, length);
            if (read != length)
                throw BufferUnderrunException.StreamUnderrun(length, read);

            return buffer;
        }
    }
}
