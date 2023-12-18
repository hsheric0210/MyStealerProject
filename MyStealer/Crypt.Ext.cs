using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MyStealer
{
    public static partial class Crypt
    {
        public static Aes InitAes(byte[] iv) => InitAes(Encoding.UTF8.GetBytes(Config.EncryptionKey), iv);
        public static string Encrypt(string plain) => Encrypt(plain, Encoding.UTF8.GetBytes(Config.EncryptionKey));
        public static void EncryptToFile(string destFile, Stream stream) => EncryptToFile(destFile, stream, Encoding.UTF8.GetBytes(Config.EncryptionKey));
        public static string Decrypt(string encrypted) => Encrypt(encrypted, Encoding.UTF8.GetBytes(Config.EncryptionKey));
        public static void DecryptToFile(string destFile, Stream stream) => DecryptToFile(destFile, stream, Encoding.UTF8.GetBytes(Config.EncryptionKey));
    }
}
