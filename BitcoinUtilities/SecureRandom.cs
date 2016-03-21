using System;
using System.Diagnostics.Contracts;

namespace BitcoinUtilities
{
    /*
        The seed and data must be generated differently.
        Otherwise, it would be possible to determine the seed from the generated random values.

        Several values for noise are used to make sure that inputs for hash functions are different in more than one bit.

        SHA-512 is used for seed generation.
        SHA-256 is used for data generation, instead of SHA-512, to avoid splitting SHA-512 to several 256-keys.
    */

    /// <summary>
    /// A pseudo-random number generator based on hash functions.
    /// </summary>
    public class SecureRandom
    {
        private const int MaxSeedUsages = 31;

        // Numbers for SeedNoise were taken from SHA-256 hash of string 'seed'.
        private static readonly uint[] SeedNoise = new uint[]
        {
            0x19B25856, 0xE1C150CA, 0x834CFFC8, 0xB59B23AD,
            0xBD0EC038, 0x9E58EB22, 0xB3B64768
        };

        // Numbers for DataNoise were taken from SHA-256 hash of string 'data'.
        private static readonly uint[] DataNoise = new uint[]
        {
            0x3A6EB079, 0x0F39AC87, 0xC94F3856, 0xB2DD2C5D,
            0x110E6811, 0x602261A9, 0xA923D3BB
        };

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

            dataBuffer = new byte[0];
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
        /// </summary>
        /// <remarks>
        /// This method should be used only for testing purposes.
        /// </remarks>
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
            byte[] noise = GetNoise(SeedNoise, seedVersion);
            seed = CryptoUtils.Sha512(seed, seedMaterial, noise, NumberUtils.GetBytes(seedVersion));

            dataBuffer = new byte[0];
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
            if (dataBufferOffset >= dataBuffer.Length)
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
            byte[] noise = GetNoise(DataNoise, dataBufferVersion);
            dataBuffer = CryptoUtils.Sha256(dataBuffer, seed, noise, NumberUtils.GetBytes(dataBufferVersion));
        }

        private void CreateNextSeed()
        {
            seedUsages = 0;
            seedVersion++;
            byte[] noise = GetNoise(SeedNoise, seedVersion);
            seed = CryptoUtils.Sha512(seed, noise, NumberUtils.GetBytes(seedVersion));

            dataBuffer = new byte[0];
        }

        private byte[] GetNoise(uint[] noises, int version)
        {
            return NumberUtils.GetBytes((int) noises[version%noises.Length]);
        }
    }
}