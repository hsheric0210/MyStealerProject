using System;

namespace MyStealer.Utils.BcAesGcm
{
#pragma warning disable CS0618 // Type or member is obsolete
    /// <summary>
    /// Implements the Galois/Counter mode (GCM) detailed in NIST Special Publication 800-38D.
    /// </summary>
    public sealed class GcmBlockCipher
        : IAeadBlockCipher
    {
        public static IGcmMultiplier CreateGcmMultiplier()
        {
            if (BasicGcmMultiplier.IsHardwareAccelerated)
                return new BasicGcmMultiplier();

            return new Tables4kGcmMultiplier();
        }

        private const int BlockSize = 16;

        private readonly IBlockCipher cipher;
        private readonly IGcmMultiplier multiplier;
        private IGcmExponentiator exp;

        // These fields are set by Init and not modified by processing
        private bool forEncryption;
        private bool initialised;
        private int macSize;
        private byte[] lastKey;
        private byte[] nonce;
        private byte[] initialAssociatedText;
        private byte[] H;
        private byte[] J0;

        // These fields are modified during processing
        private byte[] bufBlock;
        private byte[] macBlock;
        private byte[] S, S_at, S_atPre;
        private byte[] counter;
        private uint counter32;
        private uint blocksRemaining;
        private int bufOff;
        private ulong totalLength;
        private byte[] atBlock;
        private int atBlockPos;
        private ulong atLength;
        private ulong atLengthPre;

        public GcmBlockCipher(
            IBlockCipher c)
            : this(c, null)
        {
        }

        [Obsolete("Will be removed")]
        public GcmBlockCipher(
            IBlockCipher c,
            IGcmMultiplier m)
        {
            if (c.GetBlockSize() != BlockSize)
                throw new ArgumentException("cipher required with a block size of " + BlockSize + ".");

            if (m == null)
                m = CreateGcmMultiplier();

            cipher = c;
            multiplier = m;
        }

        public string AlgorithmName => cipher.AlgorithmName + "/GCM";

        public IBlockCipher UnderlyingCipher => cipher;

        public int GetBlockSize() => BlockSize;

        /// <remarks>
        /// MAC sizes from 32 bits to 128 bits (must be a multiple of 8) are supported. The default is 128 bits.
        /// Sizes less than 96 are not recommended, but are supported for specialized applications.
        /// </remarks>
        public void Init(bool forEncryption, ICipherParameters parameters)
        {
            this.forEncryption = forEncryption;
            macBlock = null;
            initialised = true;

            KeyParameter keyParam;
            byte[] newNonce;

            if (parameters is AeadParameters aeadParameters)
            {
                newNonce = aeadParameters.GetNonce();
                initialAssociatedText = aeadParameters.GetAssociatedText();

                var macSizeBits = aeadParameters.MacSize;
                if (macSizeBits < 32 || macSizeBits > 128 || macSizeBits % 8 != 0)
                    throw new ArgumentException("Invalid value for MAC size: " + macSizeBits);

                macSize = macSizeBits / 8;
                keyParam = aeadParameters.Key;
            }
            else if (parameters is ParametersWithIV withIV)
            {
                newNonce = withIV.GetIV();
                initialAssociatedText = null;
                macSize = 16;
                keyParam = (KeyParameter)withIV.Parameters;
            }
            else
            {
                throw new ArgumentException("invalid parameters passed to GCM");
            }

            var bufLength = forEncryption ? BlockSize : BlockSize + macSize;
            bufBlock = new byte[bufLength];

            if (newNonce.Length < 1)
                throw new ArgumentException("IV must be at least 1 byte");

            if (forEncryption)
            {
                if (nonce != null && Arrays.AreEqual(nonce, newNonce))
                {
                    if (keyParam == null)
                        throw new ArgumentException("cannot reuse nonce for GCM encryption");

                    if (lastKey != null && keyParam.FixedTimeEquals(lastKey))
                        throw new ArgumentException("cannot reuse nonce for GCM encryption");
                }
            }

            nonce = newNonce;
            if (keyParam != null)
                lastKey = keyParam.GetKey();

            // TODO Restrict macSize to 16 if nonce length not 12?

            // Cipher always used in forward mode
            // if keyParam is null we're reusing the last key.
            if (keyParam != null)
            {
                cipher.Init(true, keyParam);

                H = new byte[BlockSize];
                cipher.ProcessBlock(H, 0, H, 0);

                // if keyParam is null we're reusing the last key and the multiplier doesn't need re-init
                multiplier.Init(H);
                exp = null;
            }
            else if (H == null)
            {
                throw new ArgumentException("Key must be specified in initial Init");
            }

            J0 = new byte[BlockSize];

            if (nonce.Length == 12)
            {
                Array.Copy(nonce, 0, J0, 0, nonce.Length);
                J0[BlockSize - 1] = 0x01;
            }
            else
            {
                gHASH(J0, nonce, nonce.Length);
                var X = new byte[BlockSize];
                Pack.UInt64_To_BE((ulong)nonce.Length * 8UL, X, 8);
                gHASHBlock(J0, X);
            }

            S = new byte[BlockSize];
            S_at = new byte[BlockSize];
            S_atPre = new byte[BlockSize];
            atBlock = new byte[BlockSize];
            atBlockPos = 0;
            atLength = 0;
            atLengthPre = 0;
            counter = Arrays.Clone(J0);
            counter32 = Pack.BE_To_UInt32(counter, 12);
            blocksRemaining = uint.MaxValue - 1; // page 8, len(P) <= 2^39 - 256, 1 block used by tag
            bufOff = 0;
            totalLength = 0;

            if (initialAssociatedText != null)
                ProcessAadBytes(initialAssociatedText, 0, initialAssociatedText.Length);
        }

        public byte[] GetMac() => macBlock == null ? new byte[macSize] : (byte[])macBlock.Clone();

        public int GetOutputSize(int len)
        {
            var totalData = len + bufOff;

            if (forEncryption)
                return totalData + macSize;

            return totalData < macSize ? 0 : totalData - macSize;
        }

        public int GetUpdateOutputSize(int len)
        {
            var totalData = len + bufOff;
            if (!forEncryption)
            {
                if (totalData < macSize)
                    return 0;

                totalData -= macSize;
            }
            return totalData - totalData % BlockSize;
        }

        public void ProcessAadByte(byte input)
        {
            CheckStatus();

            atBlock[atBlockPos] = input;
            if (++atBlockPos == BlockSize)
            {
                // Hash each block as it fills
                gHASHBlock(S_at, atBlock);
                atBlockPos = 0;
                atLength += BlockSize;
            }
        }

        public void ProcessAadBytes(byte[] inBytes, int inOff, int len)
        {
            CheckStatus();

            if (atBlockPos > 0)
            {
                var available = BlockSize - atBlockPos;
                if (len < available)
                {
                    Array.Copy(inBytes, inOff, atBlock, atBlockPos, len);
                    atBlockPos += len;
                    return;
                }

                Array.Copy(inBytes, inOff, atBlock, atBlockPos, available);
                gHASHBlock(S_at, atBlock);
                atLength += BlockSize;
                inOff += available;
                len -= available;
                //atBlockPos = 0;
            }

            var inLimit = inOff + len - BlockSize;

            while (inOff <= inLimit)
            {
                gHASHBlock(S_at, inBytes, inOff);
                atLength += BlockSize;
                inOff += BlockSize;
            }

            atBlockPos = BlockSize + inLimit - inOff;
            Array.Copy(inBytes, inOff, atBlock, 0, atBlockPos);
        }

        private void InitCipher()
        {
            if (atLength > 0)
            {
                Array.Copy(S_at, 0, S_atPre, 0, BlockSize);
                atLengthPre = atLength;
            }

            // Finish hash for partial AAD block
            if (atBlockPos > 0)
            {
                gHASHPartial(S_atPre, atBlock, 0, atBlockPos);
                atLengthPre += (uint)atBlockPos;
            }

            if (atLengthPre > 0)
                Array.Copy(S_atPre, 0, S, 0, BlockSize);
        }

        public int ProcessByte(byte input, byte[] output, int outOff)
        {
            CheckStatus();

            bufBlock[bufOff] = input;
            if (++bufOff == bufBlock.Length)
            {
                Check.OutputLength(output, outOff, BlockSize, "output buffer too short");

                if (blocksRemaining == 0)
                    throw new InvalidOperationException("Attempt to process too many blocks");

                --blocksRemaining;

                if (totalLength == 0)
                    InitCipher();

                if (forEncryption)
                {
                    EncryptBlock(bufBlock, 0, output, outOff);
                    bufOff = 0;
                }
                else
                {
                    DecryptBlock(bufBlock, 0, output, outOff);
                    Array.Copy(bufBlock, BlockSize, bufBlock, 0, macSize);
                    bufOff = macSize;
                }

                totalLength += BlockSize;
                return BlockSize;
            }
            return 0;
        }

        public int ProcessBytes(byte[] input, int inOff, int len, byte[] output, int outOff)
        {
            CheckStatus();

            Check.DataLength(input, inOff, len, "input buffer too short");

            var resultLen = bufOff + len;

            if (forEncryption)
            {
                resultLen &= -BlockSize;
                if (resultLen > 0)
                {
                    Check.OutputLength(output, outOff, resultLen, "output buffer too short");

                    var blocksNeeded = (uint)resultLen >> 4;
                    if (blocksRemaining < blocksNeeded)
                        throw new InvalidOperationException("Attempt to process too many blocks");

                    blocksRemaining -= blocksNeeded;

                    if (totalLength == 0)
                        InitCipher();
                }

                if (bufOff > 0)
                {
                    var available = BlockSize - bufOff;
                    if (len < available)
                    {
                        Array.Copy(input, inOff, bufBlock, bufOff, len);
                        bufOff += len;
                        return 0;
                    }

                    Array.Copy(input, inOff, bufBlock, bufOff, available);
                    inOff += available;
                    len -= available;

                    EncryptBlock(bufBlock, 0, output, outOff);
                    outOff += BlockSize;

                    //bufOff = 0;
                }

                var inLimit1 = inOff + len - BlockSize;
                var inLimit2 = inLimit1 - BlockSize;

                while (inOff <= inLimit2)
                {
                    EncryptBlocks2(input, inOff, output, outOff);
                    inOff += BlockSize * 2;
                    outOff += BlockSize * 2;
                }

                if (inOff <= inLimit1)
                {
                    EncryptBlock(input, inOff, output, outOff);
                    inOff += BlockSize;
                    //outOff += BlockSize;
                }

                bufOff = BlockSize + inLimit1 - inOff;
                Array.Copy(input, inOff, bufBlock, 0, bufOff);
            }
            else
            {
                resultLen -= macSize;
                resultLen &= -BlockSize;
                if (resultLen > 0)
                {
                    Check.OutputLength(output, outOff, resultLen, "output buffer too short");

                    var blocksNeeded = (uint)resultLen >> 4;
                    if (blocksRemaining < blocksNeeded)
                        throw new InvalidOperationException("Attempt to process too many blocks");

                    blocksRemaining -= blocksNeeded;

                    if (totalLength == 0)
                        InitCipher();
                }

                var available = bufBlock.Length - bufOff;
                if (len < available)
                {
                    Array.Copy(input, inOff, bufBlock, bufOff, len);
                    bufOff += len;
                    return 0;
                }

                if (bufOff >= BlockSize)
                {
                    DecryptBlock(bufBlock, 0, output, outOff);
                    outOff += BlockSize;

                    bufOff -= BlockSize;
                    Array.Copy(bufBlock, BlockSize, bufBlock, 0, bufOff);

                    available += BlockSize;
                    if (len < available)
                    {
                        Array.Copy(input, inOff, bufBlock, bufOff, len);
                        bufOff += len;

                        totalLength += BlockSize;
                        return BlockSize;
                    }
                }

                var inLimit1 = inOff + len - bufBlock.Length;
                var inLimit2 = inLimit1 - BlockSize;

                available = BlockSize - bufOff;
                Array.Copy(input, inOff, bufBlock, bufOff, available);
                inOff += available;

                DecryptBlock(bufBlock, 0, output, outOff);
                outOff += BlockSize;
                //bufOff = 0;

                while (inOff <= inLimit2)
                {
                    DecryptBlocks2(input, inOff, output, outOff);
                    inOff += BlockSize * 2;
                    outOff += BlockSize * 2;
                }

                if (inOff <= inLimit1)
                {
                    DecryptBlock(input, inOff, output, outOff);
                    inOff += BlockSize;
                    //outOff += BlockSize;
                }

                bufOff = bufBlock.Length + inLimit1 - inOff;
                Array.Copy(input, inOff, bufBlock, 0, bufOff);
            }

            totalLength += (uint)resultLen;
            return resultLen;
        }

        public int DoFinal(byte[] output, int outOff)
        {
            CheckStatus();

            var extra = bufOff;

            if (forEncryption)
                Check.OutputLength(output, outOff, extra + macSize, "output buffer too short");
            else
            {
                if (extra < macSize)
                    throw new InvalidCipherTextException("data too short");

                extra -= macSize;

                Check.OutputLength(output, outOff, extra, "output buffer too short");
            }

            if (totalLength == 0)
                InitCipher();

            if (extra > 0)
            {
                if (blocksRemaining == 0)
                    throw new InvalidOperationException("Attempt to process too many blocks");

                --blocksRemaining;

                ProcessPartial(bufBlock, 0, extra, output, outOff);
            }

            atLength += (uint)atBlockPos;

            if (atLength > atLengthPre)
            {
                /*
                 *  Some AAD was sent after the cipher started. We determine the difference b/w the hash value
                 *  we actually used when the cipher started (S_atPre) and the final hash value calculated (S_at).
                 *  Then we carry this difference forward by multiplying by H^c, where c is the number of (full or
                 *  partial) cipher-text blocks produced, and adjust the current hash.
                 */

                // Finish hash for partial AAD block
                if (atBlockPos > 0)
                    gHASHPartial(S_at, atBlock, 0, atBlockPos);

                // Find the difference between the AAD hashes
                if (atLengthPre > 0)
                    GcmUtilities.Xor(S_at, S_atPre);

                // Number of cipher-text blocks produced
                var c = (long)(totalLength * 8 + 127 >> 7);

                // Calculate the adjustment factor
                var H_c = new byte[16];
                if (exp == null)
                {
                    exp = new BasicGcmExponentiator();
                    exp.Init(H);
                }
                exp.ExponentiateX(c, H_c);

                // Carry the difference forward
                GcmUtilities.Multiply(S_at, H_c);

                // Adjust the current hash
                GcmUtilities.Xor(S, S_at);
            }

            // Final gHASH
            var X = new byte[BlockSize];
            Pack.UInt64_To_BE(atLength * 8UL, X, 0);
            Pack.UInt64_To_BE(totalLength * 8UL, X, 8);

            gHASHBlock(S, X);

            // T = MSBt(GCTRk(J0,S))
            var tag = new byte[BlockSize];
            cipher.ProcessBlock(J0, 0, tag, 0);
            GcmUtilities.Xor(tag, S);

            var resultLen = extra;

            // We place into macBlock our calculated value for T
            macBlock = new byte[macSize];
            Array.Copy(tag, 0, macBlock, 0, macSize);

            if (forEncryption)
            {
                // Append T to the message
                Array.Copy(macBlock, 0, output, outOff + bufOff, macSize);
                resultLen += macSize;
            }
            else
            {
                // Retrieve the T value from the message and compare to calculated one
                var msgMac = new byte[macSize];
                Array.Copy(bufBlock, extra, msgMac, 0, macSize);
                if (!Arrays.FixedTimeEquals(macBlock, msgMac))
                    throw new InvalidCipherTextException("mac check in GCM failed");
            }

            Reset(false);

            return resultLen;
        }

        public void Reset() => Reset(true);

        private void Reset(bool clearMac)
        {
            // note: we do not reset the nonce.

            S = new byte[BlockSize];
            S_at = new byte[BlockSize];
            S_atPre = new byte[BlockSize];
            atBlock = new byte[BlockSize];
            atBlockPos = 0;
            atLength = 0;
            atLengthPre = 0;
            counter = Arrays.Clone(J0);
            counter32 = Pack.BE_To_UInt32(counter, 12);
            blocksRemaining = uint.MaxValue - 1;
            bufOff = 0;
            totalLength = 0;

            if (bufBlock != null)
                Arrays.Fill(bufBlock, 0);

            if (clearMac)
                macBlock = null;

            if (forEncryption)
                initialised = false;
            else if (initialAssociatedText != null)
            {
                ProcessAadBytes(initialAssociatedText, 0, initialAssociatedText.Length);
            }
        }

        private void DecryptBlock(byte[] inBuf, int inOff, byte[] outBuf, int outOff)
        {
            var ctrBlock = new byte[BlockSize];

            GetNextCtrBlock(ctrBlock);
            {
                for (var i = 0; i < BlockSize; i += 4)
                {
                    var c0 = inBuf[inOff + i + 0];
                    var c1 = inBuf[inOff + i + 1];
                    var c2 = inBuf[inOff + i + 2];
                    var c3 = inBuf[inOff + i + 3];

                    S[i + 0] ^= c0;
                    S[i + 1] ^= c1;
                    S[i + 2] ^= c2;
                    S[i + 3] ^= c3;

                    outBuf[outOff + i + 0] = (byte)(c0 ^ ctrBlock[i + 0]);
                    outBuf[outOff + i + 1] = (byte)(c1 ^ ctrBlock[i + 1]);
                    outBuf[outOff + i + 2] = (byte)(c2 ^ ctrBlock[i + 2]);
                    outBuf[outOff + i + 3] = (byte)(c3 ^ ctrBlock[i + 3]);
                }
            }
            multiplier.MultiplyH(S);
        }

        private void DecryptBlocks2(byte[] inBuf, int inOff, byte[] outBuf, int outOff)
        {
            var ctrBlock = new byte[BlockSize];

            GetNextCtrBlock(ctrBlock);
            {
                for (var i = 0; i < BlockSize; i += 4)
                {
                    var c0 = inBuf[inOff + i + 0];
                    var c1 = inBuf[inOff + i + 1];
                    var c2 = inBuf[inOff + i + 2];
                    var c3 = inBuf[inOff + i + 3];

                    S[i + 0] ^= c0;
                    S[i + 1] ^= c1;
                    S[i + 2] ^= c2;
                    S[i + 3] ^= c3;

                    outBuf[outOff + i + 0] = (byte)(c0 ^ ctrBlock[i + 0]);
                    outBuf[outOff + i + 1] = (byte)(c1 ^ ctrBlock[i + 1]);
                    outBuf[outOff + i + 2] = (byte)(c2 ^ ctrBlock[i + 2]);
                    outBuf[outOff + i + 3] = (byte)(c3 ^ ctrBlock[i + 3]);
                }
            }
            multiplier.MultiplyH(S);

            inOff += BlockSize;
            outOff += BlockSize;

            GetNextCtrBlock(ctrBlock);
            {
                for (var i = 0; i < BlockSize; i += 4)
                {
                    var c0 = inBuf[inOff + i + 0];
                    var c1 = inBuf[inOff + i + 1];
                    var c2 = inBuf[inOff + i + 2];
                    var c3 = inBuf[inOff + i + 3];

                    S[i + 0] ^= c0;
                    S[i + 1] ^= c1;
                    S[i + 2] ^= c2;
                    S[i + 3] ^= c3;

                    outBuf[outOff + i + 0] = (byte)(c0 ^ ctrBlock[i + 0]);
                    outBuf[outOff + i + 1] = (byte)(c1 ^ ctrBlock[i + 1]);
                    outBuf[outOff + i + 2] = (byte)(c2 ^ ctrBlock[i + 2]);
                    outBuf[outOff + i + 3] = (byte)(c3 ^ ctrBlock[i + 3]);
                }
            }
            multiplier.MultiplyH(S);
        }

        private void EncryptBlock(byte[] inBuf, int inOff, byte[] outBuf, int outOff)
        {
            var ctrBlock = new byte[BlockSize];

            GetNextCtrBlock(ctrBlock);
            {
                for (var i = 0; i < BlockSize; i += 4)
                {
                    var c0 = (byte)(ctrBlock[i + 0] ^ inBuf[inOff + i + 0]);
                    var c1 = (byte)(ctrBlock[i + 1] ^ inBuf[inOff + i + 1]);
                    var c2 = (byte)(ctrBlock[i + 2] ^ inBuf[inOff + i + 2]);
                    var c3 = (byte)(ctrBlock[i + 3] ^ inBuf[inOff + i + 3]);

                    S[i + 0] ^= c0;
                    S[i + 1] ^= c1;
                    S[i + 2] ^= c2;
                    S[i + 3] ^= c3;

                    outBuf[outOff + i + 0] = c0;
                    outBuf[outOff + i + 1] = c1;
                    outBuf[outOff + i + 2] = c2;
                    outBuf[outOff + i + 3] = c3;
                }
            }
            multiplier.MultiplyH(S);
        }

        private void EncryptBlocks2(byte[] inBuf, int inOff, byte[] outBuf, int outOff)
        {
            var ctrBlock = new byte[BlockSize];

            GetNextCtrBlock(ctrBlock);
            {
                for (var i = 0; i < BlockSize; i += 4)
                {
                    var c0 = (byte)(ctrBlock[i + 0] ^ inBuf[inOff + i + 0]);
                    var c1 = (byte)(ctrBlock[i + 1] ^ inBuf[inOff + i + 1]);
                    var c2 = (byte)(ctrBlock[i + 2] ^ inBuf[inOff + i + 2]);
                    var c3 = (byte)(ctrBlock[i + 3] ^ inBuf[inOff + i + 3]);

                    S[i + 0] ^= c0;
                    S[i + 1] ^= c1;
                    S[i + 2] ^= c2;
                    S[i + 3] ^= c3;

                    outBuf[outOff + i + 0] = c0;
                    outBuf[outOff + i + 1] = c1;
                    outBuf[outOff + i + 2] = c2;
                    outBuf[outOff + i + 3] = c3;
                }
            }
            multiplier.MultiplyH(S);

            inOff += BlockSize;
            outOff += BlockSize;

            GetNextCtrBlock(ctrBlock);
            {
                for (var i = 0; i < BlockSize; i += 4)
                {
                    var c0 = (byte)(ctrBlock[i + 0] ^ inBuf[inOff + i + 0]);
                    var c1 = (byte)(ctrBlock[i + 1] ^ inBuf[inOff + i + 1]);
                    var c2 = (byte)(ctrBlock[i + 2] ^ inBuf[inOff + i + 2]);
                    var c3 = (byte)(ctrBlock[i + 3] ^ inBuf[inOff + i + 3]);

                    S[i + 0] ^= c0;
                    S[i + 1] ^= c1;
                    S[i + 2] ^= c2;
                    S[i + 3] ^= c3;

                    outBuf[outOff + i + 0] = c0;
                    outBuf[outOff + i + 1] = c1;
                    outBuf[outOff + i + 2] = c2;
                    outBuf[outOff + i + 3] = c3;
                }
            }
            multiplier.MultiplyH(S);
        }

        private void GetNextCtrBlock(byte[] block)
        {
            Pack.UInt32_To_BE(++counter32, counter, 12);

            cipher.ProcessBlock(counter, 0, block, 0);
        }

        private void ProcessPartial(byte[] buf, int off, int len, byte[] output, int outOff)
        {
            var ctrBlock = new byte[BlockSize];
            GetNextCtrBlock(ctrBlock);

            if (forEncryption)
            {
                GcmUtilities.Xor(buf, off, ctrBlock, 0, len);
                gHASHPartial(S, buf, off, len);
            }
            else
            {
                gHASHPartial(S, buf, off, len);
                GcmUtilities.Xor(buf, off, ctrBlock, 0, len);
            }

            Array.Copy(buf, off, output, outOff, len);
            totalLength += (uint)len;
        }

        private void gHASH(byte[] Y, byte[] b, int len)
        {
            for (var pos = 0; pos < len; pos += BlockSize)
            {
                var num = Math.Min(len - pos, BlockSize);
                gHASHPartial(Y, b, pos, num);
            }
        }

        private void gHASHBlock(byte[] Y, byte[] b)
        {
            GcmUtilities.Xor(Y, b);
            multiplier.MultiplyH(Y);
        }

        private void gHASHBlock(byte[] Y, byte[] b, int off)
        {
            GcmUtilities.Xor(Y, b, off);
            multiplier.MultiplyH(Y);
        }

        private void gHASHPartial(byte[] Y, byte[] b, int off, int len)
        {
            GcmUtilities.Xor(Y, b, off, len);
            multiplier.MultiplyH(Y);
        }

        private void CheckStatus()
        {
            if (!initialised)
            {
                if (forEncryption)
                    throw new InvalidOperationException("GCM cipher cannot be reused for encryption");

                throw new InvalidOperationException("GCM cipher needs to be initialized");
            }
        }
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
