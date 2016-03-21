using System;
using System.Diagnostics.Contracts;

namespace BitcoinUtilities
{
    /// <summary>
    /// A pseudo-random number generator based on the SHA-512 hash function.
    /// </summary>
    public class SecureRandom
    {
        private const int MaxSeedUsages = 31;

        // SeedSalt and DataSalt must be different.
        // Otherwise, it would be possible to determine the seed from the generated random values.
        private static readonly byte[] SeedSalt = new byte[] {0xAA, 0xBB, 0xCC, 0xDD};
        private static readonly byte[] DataSalt = new byte[] {0x11, 0x22, 0x33, 0x44};

        private byte[] seed;
        private int seedUsages;
        private int seedVersion;

        private byte[] dataBuffer;
        private int dataBufferVersion;
        private int dataBufferOffset;

        /// <summary>
        /// Initializes a new instance of SecureRandom, using the given seed.
        /// </summary>
        private SecureRandom(byte[] seed)
        {
            this.seed = seed;
            seedUsages = 0;
            seedVersion = 0;
        }

        /// <summary>
        /// Creates a new instance of SecureRandom, using a unique set of parameters for seed generation.
        /// </summary>
        /// <returns>A new instance of SecureRandom.</returns>
        public static SecureRandom Create()
        {
            byte[] seed = SecureRandomSeedGenerator.CreateSeed();

            return new SecureRandom(seed);
        }

        /// <summary>
        /// Creates a new instance of SecureRandom with empty seed.
        /// <para/>
        /// Should be used only for testing purposes.
        /// </summary>
        /// <returns>A new instance of SecureRandom.</returns>
        internal static SecureRandom CreateEmpty()
        {
            return new SecureRandom(new byte[0]);
        }

        /// <summary>
        /// Generates a new seed using old seed and the given seed material.
        /// </summary>
        /// <returns>A new instance of SecureRandom.</returns>
        public void AddSeedMaterial(byte[] seedMaterial)
        {
            seedUsages = 0;
            seedVersion++;
            seed = CryptoUtils.Sha512(seed, seedMaterial, SeedSalt, NumberUtils.GetBytes(seedVersion));

            dataBuffer = null;
        }

        /// <summary>
        /// Creates an array of random bytes with the given length.
        /// </summary>
        /// <param name="length">The required length of array.</param>
        public byte[] NextBytes(int length)
        {
            byte[] buffer = new byte[length];
            int offset = 0;
            while (offset < length)
            {
                offset += WriteBytes(buffer, offset, length - offset);
            }
            return buffer;
        }

        private int WriteBytes(byte[] buffer, int offset, int length)
        {
            if (dataBuffer == null || dataBufferOffset == dataBuffer.Length)
            {
                FillBuffer();
                Contract.Assert(dataBuffer != null);
            }

            int bytesWritten = Math.Min(length, dataBuffer.Length - dataBufferOffset);

            Array.Copy(dataBuffer, dataBufferOffset, buffer, offset, bytesWritten);

            dataBufferOffset += bytesWritten;

            return bytesWritten;
        }

        private void FillBuffer()
        {
            if (seedUsages >= MaxSeedUsages)
            {
                CreateNextSeed();
            }

            seedUsages++;

            dataBufferOffset = 0;
            dataBufferVersion++;
            dataBuffer = CryptoUtils.Sha512(seed, DataSalt, NumberUtils.GetBytes(dataBufferVersion));
        }

        private void CreateNextSeed()
        {
            seedUsages = 0;
            seedVersion++;
            seed = CryptoUtils.Sha512(seed, SeedSalt, NumberUtils.GetBytes(seedVersion));

            dataBuffer = null;
        }
    }
}