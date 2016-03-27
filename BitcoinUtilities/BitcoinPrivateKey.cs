using System.Collections.Generic;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace BitcoinUtilities
{
    /// <summary>
    /// Specification: https://en.bitcoin.it/wiki/Private_key
    /// </summary>
    public static class BitcoinPrivateKey
    {
        public const int KeyLength = 32;

        private static readonly byte[] MinValue = new byte[]
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01
        };

        private static readonly byte[] MaxValue = new byte[]
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFE,
            0xBA, 0xAE, 0xDC, 0xE6, 0xAF, 0x48, 0xA0, 0x3B, 0xBF, 0xD2, 0x5E, 0x8C, 0xD0, 0x36, 0x41, 0x40
        };

        /// <summary>
        /// Creates a valid private key that passes <see cref="IsStrong"/> checks.
        /// </summary>
        /// <param name="random">The source of random data for the key.</param>
        /// <returns>An array of 32 bytes with the generated private key.</returns>
        public static byte[] Create(SecureRandom random)
        {
            bool goodKey = false;
            byte[] privateKey = null;

            while (!goodKey)
            {
                privateKey = random.NextBytes(32);
                goodKey = IsStrong(privateKey);
            }

            return privateKey;
        }

        /// <summary>
        /// Checks that the given array is a valid private key.
        /// </summary>
        /// <param name="privateKey">The array of 32 bytes to validate.</param>
        /// <returns>true if the given byte array is a valid private key; otherwise, false.</returns>
        public static bool IsValid(byte[] privateKey)
        {
            if (privateKey == null)
            {
                return false;
            }
            if (privateKey.Length != KeyLength)
            {
                return false;
            }
            if (Compare(privateKey, MinValue) < 0)
            {
                return false;
            }
            if (Compare(privateKey, MaxValue) > 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Performs checks on the given private key to ensure that it looks random.
        /// </summary>
        /// <param name="privateKey">The array of 32 bytes to validate.</param>
        /// <returns>true if the given byte array is a valid private key that looks random; otherwise, false.</returns>
        public static bool IsStrong(byte[] privateKey)
        {
            if (!IsValid(privateKey))
            {
                return false;
            }

            if (!IsRandomArray(privateKey))
            {
                return false;
            }

            var privateKeyNumber = new BigInteger(1, privateKey);
            var invPrivateKeyNumber = privateKeyNumber.ModInverse(Secp256K1Curve.GroupSize);
            byte[] invPrivateKey = invPrivateKeyNumber.ToByteArrayUnsigned();

            if (!IsRandomArray(invPrivateKey))
            {
                return false;
            }

            return true;
        }

        //todo: add XMLDOC
        public static byte[] ToEncodedPublicKey(byte[] privateKey, bool compressed)
        {
            ECPoint publicKeyPoint = Secp256K1Curve.G.Multiply(new BigInteger(1, privateKey));

            // Compressed and uncopressed formats are defined in "SEC 1: Elliptic Curve Cryptography" in section "2.3.3 Elliptic-Curve-Point-to-Octet-String Conversion".
            // see: http://www.secg.org/sec1-v2.pdf
            ECPoint publicKey = publicKeyPoint.Curve.CreatePoint(publicKeyPoint.X.ToBigInteger(), publicKeyPoint.Y.ToBigInteger(), compressed);
            return publicKey.GetEncoded();
        }

        private static bool IsRandomArray(byte[] privateKey)
        {
            HashSet<byte> distinctBytes = new HashSet<byte>();
            HashSet<byte> distinctDeltas = new HashSet<byte>();
            for (int i = 0; i < privateKey.Length; i++)
            {
                byte b0 = privateKey[i];
                byte b1 = privateKey[(i + 1)%privateKey.Length];
                byte delta = (byte) (b1 - b0);

                distinctBytes.Add(b0);
                distinctDeltas.Add(delta);
            }

            if (distinctBytes.Count < 24)
            {
                return false;
            }

            if (distinctDeltas.Count < 24)
            {
                return false;
            }

            return true;
        }

        private static int Compare(byte[] privateKey1, byte[] privateKey2)
        {
            for (int i = 0; i < KeyLength; i++)
            {
                if (privateKey1[i] < privateKey2[i])
                {
                    return -1;
                }
                if (privateKey1[i] > privateKey2[i])
                {
                    return 1;
                }
            }
            return 0;
        }
    }
}