using System;
using System.Text;

namespace MyStealer.Utils
{
    /// <summary>
    /// https://stackoverflow.com/a/311179
    /// </summary>
    public static class HexString
    {
        public static string BytesToHex(byte[] bytes)
        {
            var hex = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static byte[] HexToBytes(string hexString)
        {
            var NumberChars = hexString.Length;
            var bytes = new byte[NumberChars / 2];
            for (var i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            return bytes;
        }
    }
}
