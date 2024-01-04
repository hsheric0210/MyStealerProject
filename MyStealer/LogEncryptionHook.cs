using MyStealer.Shared;
using Serilog.Sinks.File;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MyStealer
{
    internal class LogEncryptionHook : FileLifecycleHooks, IDisposable
    {
        private Stream underlyingStream;
        private Stream cryptoStream;
        private readonly byte[] iv;
        private readonly Aes aes;
        private bool disposedValue;

        public LogEncryptionHook()
        {
            iv = MyCrypt.GenIv();
            aes = MyCryptExt.InitAes(iv);
        }

        public override Stream OnFileOpened(string path, Stream underlyingStream, Encoding encoding)
        {
        this.underlyingStream = underlyingStream;
            MyCrypt.WriteHeader(underlyingStream, iv);
            cryptoStream = new CryptoStream(underlyingStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            return cryptoStream;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cryptoStream.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
