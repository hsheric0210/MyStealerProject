using Serilog.Sinks.File;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MyStealer
{
    internal class LogEncryptionHook : FileLifecycleHooks
    {
        private readonly byte[] iv;
        private readonly Aes aes;

        public LogEncryptionHook()
        {
            iv = Crypt.GenIv();
            aes = Crypt.InitAes(iv);
        }

        public override Stream OnFileOpened(string path, Stream underlyingStream, Encoding encoding)
        {
            var outStream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            Crypt.WriteHeader(outStream, iv);
            return new CryptoStream(outStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        }
    }
}
