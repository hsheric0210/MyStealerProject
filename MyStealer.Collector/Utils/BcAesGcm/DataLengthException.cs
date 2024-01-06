using System;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace MyStealer.Utils.BcAesGcm
{
    /// <summary>This exception is thrown if a buffer that is meant to have output copied into it turns out to be too
    /// short, or if we've been given insufficient input.</summary>
    /// <remarks>
    /// In general this exception will get thrown rather than an <see cref="IndexOutOfRangeException"/>.
    /// </remarks>
    [Serializable]
    public class DataLengthException
        : CryptographicException
    {
        public DataLengthException()
        {
        }

        public DataLengthException(string message)
            : base(message)
        {
        }

        public DataLengthException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected DataLengthException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
