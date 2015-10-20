using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace BitcoinUtilities
{
    /// <summary>
    /// Specification: https://en.bitcoin.it/wiki/Technical_background_of_version_1_Bitcoin_addresses
    /// </summary>
    public static class BitcoinAddress
    {
        private static readonly X9ECParameters curveParameters = SecNamedCurves.GetByName("secp256k1");

        /// <summary>
        /// Creates a bitcoin address for the Main Network from the private key.
        /// </summary>
        /// <param name="privateKey">The array of 32 bytes of the private key.</param>
        /// <param name="useCompressedPublicKey">true to specify that the public key should have the compressed format; otherwise, false.</param>
        /// <exception cref="ArgumentException">The private key is invalid.</exception>
        /// <returns>Bitcoin address in Base58Check encoding.</returns>
        public static string FromPrivateKey(byte[] privateKey, bool useCompressedPublicKey)
        {
            if (!BitcoinPrivateKey.IsValid(privateKey))
            {
                throw new ArgumentException("The private key is invalid.", "privateKey");
            }

            ECPoint publicKeyPoint = curveParameters.G.Multiply(new BigInteger(1, privateKey));

            // Compressed and uncopressed formats are defined in "SEC 1: Elliptic Curve Cryptography" in section "2.3.3 Elliptic-Curve-Point-to-Octet-String Conversion".
            // see: http://www.secg.org/sec1-v2.pdf
            ECPoint publicKey = publicKeyPoint.Curve.CreatePoint(publicKeyPoint.X.ToBigInteger(), publicKeyPoint.Y.ToBigInteger(), useCompressedPublicKey);
            byte[] encodedPublicKey = publicKey.GetEncoded();

            return FromPublicKey(encodedPublicKey);
        }

        /// <summary>
        /// Converts a bitcoin address for the Main Network from the public key.
        /// <para/>
        /// This method does not validate the public key.
        /// </summary>
        /// <param name="publicKey">The array of bytes of the public key.</param>
        /// <returns>Bitcoin address in Base58Check encoding; or null if the public key is null.</returns>
        public static string FromPublicKey(byte[] publicKey)
        {
            if (publicKey == null)
            {
                return null;
            }

            //todo: set first byte according to Network type (0x00 for Main Network)
            byte[] addressBytes = new byte[21];
            using (SHA256 sha256Alg = SHA256.Create())
            {
                using (RIPEMD160 ripemd160Alg = RIPEMD160.Create())
                {
                    byte[] ripemd160Hash = ripemd160Alg.ComputeHash(sha256Alg.ComputeHash(publicKey));
                    Array.Copy(ripemd160Hash, 0, addressBytes, 1, 20);
                }
            }

            return Base58Check.Encode(addressBytes);
        }
    }
}