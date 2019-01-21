using System;
using Org.BouncyCastle.Math;

namespace BitcoinUtilities
{
    internal class EcdsaSignature
    {
        public BigInteger R { get; set; }
        public BigInteger S { get; set; }

        public byte[] ToDER()
        {
            //todo: compare ToByteArray and ToByteArrayUnsigned
            byte[] rBytes = R.ToByteArray();
            byte[] sBytes = S.ToByteArray();

            byte[] signatureBytes = new byte[6 + rBytes.Length + sBytes.Length];

            signatureBytes[0] = 0x30; // SEQUENCE 
            signatureBytes[1] = (byte) (4 + rBytes.Length + sBytes.Length); // SEQUENCE LENGTH

            signatureBytes[2] = 0x02; // INTEGER
            signatureBytes[3] = (byte) rBytes.Length; // INTEGER LENGTH
            Array.Copy(rBytes, 0, signatureBytes, 4, rBytes.Length);

            signatureBytes[4 + rBytes.Length] = 0x02; // INTEGER
            signatureBytes[5 + rBytes.Length] = (byte) sBytes.Length; // INTEGER LENGTH
            Array.Copy(sBytes, 0, signatureBytes, 6 + rBytes.Length, sBytes.Length);

            return signatureBytes;
        }

        /// <summary>
        /// Parses DER-encoded signature.
        /// </summary>
        /// <param name="encoded">A byte array with encoded signature.</param>
        /// <param name="ecdsaSignature">The decoded signature, or null if the given array is not a valid DER-encoded signature.</param>
        /// <returns>true if the signature was decoded successfully; otherwise, false.</returns>
        public static bool ParseDER(byte[] encoded, out EcdsaSignature ecdsaSignature)
        {
            ecdsaSignature = null;

            if (encoded == null || encoded.Length < 6)
            {
                return false;
            }

            if (encoded[0] != 0x30 || encoded[1] != encoded.Length - 2 || encoded[2] != 0x02)
            {
                return false;
            }

            int rLength = encoded[3];
            if (5 + rLength >= encoded.Length)
            {
                return false;
            }

            int sLength = encoded[5 + rLength];
            if (6 + rLength + sLength != encoded.Length)
            {
                return false;
            }

            ecdsaSignature = new EcdsaSignature();
            ecdsaSignature.R = new BigInteger(1, encoded, 4, rLength);
            ecdsaSignature.S = new BigInteger(1, encoded, 6 + rLength, sLength);

            return true;
        }
    }
}