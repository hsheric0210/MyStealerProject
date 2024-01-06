namespace MyStealer.Utils.BcAesGcm
{
    public static class Check
    {
        public static void DataLength(byte[] buf, int off, int len, string message)
        {
            if (off > buf.Length - len)
                ThrowDataLengthException(message);
        }

        public static void OutputLength(byte[] buf, int off, int len, string message)
        {
            if (off > buf.Length - len)
                ThrowOutputLengthException(message);
        }

        public static void ThrowDataLengthException(string message) => throw new DataLengthException(message);

        public static void ThrowOutputLengthException(string message) => throw new OutputLengthException(message);
    }
}
