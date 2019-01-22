using System;
using Org.BouncyCastle.Math;

namespace BitcoinUtilities
{
    /// <summary>
    /// A pair of R and S that represents a signature in ECDSA.
    /// </summary>
    internal class ECDSASignature
    {
        public ECDSASignature(BigInteger r, BigInteger s)
        {
            R = r;
            S = s;
        }

        public BigInteger R { get; }
        public BigInteger S { get; }

        /// <summary>
        /// Converts this signature to an ASN.1 DER sequence of two signed integers.
        /// </summary>
        /// <exception cref="InvalidOperationException">If resulting sequence has more than 127 bytes.</exception>
        public byte[] ToDER()
        {
            byte[] rBytes = R.ToByteArray();
            byte[] sBytes = S.ToByteArray();

            int sequenceLength = 4 + rBytes.Length + sBytes.Length;
            if (sequenceLength > 0x7F)
            {
                throw new InvalidOperationException($"{nameof(ECDSASignature)} does not support sequences that are longer than 127 bytes.");
            }

            byte[] signatureBytes = new byte[2 + sequenceLength];

            signatureBytes[0] = 0x30; // SEQUENCE 
            signatureBytes[1] = (byte) sequenceLength; // SEQUENCE LENGTH

            signatureBytes[2] = 0x02; // INTEGER
            signatureBytes[3] = (byte) rBytes.Length; // INTEGER LENGTH
            Array.Copy(rBytes, 0, signatureBytes, 4, rBytes.Length);

            signatureBytes[4 + rBytes.Length] = 0x02; // INTEGER
            signatureBytes[5 + rBytes.Length] = (byte) sBytes.Length; // INTEGER LENGTH
            Array.Copy(sBytes, 0, signatureBytes, 6 + rBytes.Length, sBytes.Length);

            return signatureBytes;
        }

        /// <summary>
        /// Parses DER-encoded signature. Does not support sequences that are longer than 127 bytes.
        /// </summary>
        /// <param name="encoded">A byte array with encoded signature.</param>
        /// <param name="ecdsaSignature">The decoded signature, or null if the given array is not a valid DER-encoded signature.</param>
        /// <returns>true if the signature was decoded successfully; otherwise, false.</returns>
        public static bool ParseDER(byte[] encoded, out ECDSASignature ecdsaSignature)
        {
            ecdsaSignature = null;

            if (encoded == null || encoded.Length < 6)
            {
                return false;
            }

            if (encoded[0] != 0x30)
            {
                return false;
            }

            int seqLength = encoded[1];
            if (seqLength > 0x7F || seqLength != encoded.Length - 2 || encoded[2] != 0x02)
            {
                return false;
            }

            int rLength = encoded[3];
            if (rLength == 0 || rLength > 0x7F || 5 + rLength >= encoded.Length)
            {
                return false;
            }

            if (encoded[4 + rLength] != 0x02)
            {
                return false;
            }

            int sLength = encoded[5 + rLength];
            if (sLength == 0 || sLength > 0x7F || 6 + rLength + sLength != encoded.Length)
            {
                return false;
            }

            if (rLength > 1)
            {
                if (encoded[4] == 0 && encoded[5] <= 0x7F)
                {
                    return false;
                }

                if (encoded[4] == 0xFF && encoded[5] >= 0x80)
                {
                    return false;
                }
            }

            if (sLength > 1)
            {
                if (encoded[6 + rLength] == 0 && encoded[7 + rLength] <= 0x7F)
                {
                    return false;
                }

                if (encoded[6 + rLength] == 0xFF && encoded[7 + rLength] >= 0x80)
                {
                    return false;
                }
            }

            ecdsaSignature = new ECDSASignature
            (
                new BigInteger(encoded, 4, rLength),
                new BigInteger(encoded, 6 + rLength, sLength)
            );

            return true;
        }
    }
}