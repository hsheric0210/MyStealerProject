using MyStealer.Shared;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MyStealer
{
    public static class MyCryptExt
    {
        public static Aes InitAes(byte[] iv) => MyCrypt.InitAes(Encoding.UTF8.GetBytes(Config.EncryptionKey), iv);
        public static string Encrypt(string plain) => MyCrypt.Encrypt(plain, Encoding.UTF8.GetBytes(Config.EncryptionKey));
        public static void EncryptToFile(string destFile, Stream stream) => MyCrypt.EncryptToFile(destFile, stream, Encoding.UTF8.GetBytes(Config.EncryptionKey));
        public static string Decrypt(string encrypted) => MyCrypt.Encrypt(encrypted, Encoding.UTF8.GetBytes(Config.EncryptionKey));
        public static void DecryptToFile(string destFile, Stream stream) => MyCrypt.DecryptToFile(destFile, stream, Encoding.UTF8.GetBytes(Config.EncryptionKey));
    }
}
