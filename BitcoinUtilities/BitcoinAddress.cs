﻿using System;
using System.Security.Cryptography;
using System.Text;
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
        private static readonly X9ECParameters CurveParameters = SecNamedCurves.GetByName("secp256k1");

        /// <summary>
        /// Creates a bitcoin address for Main Network from private key.
        /// </summary>
        /// <param name="privateKey">a private key</param>
        /// <param name="useCompressedPublicKey">true to specify that the public key should have the compressed format; otherwise, false.</param>
        /// <returns></returns>
        public static string FromPrivateKey(byte[] privateKey, bool useCompressedPublicKey)
        {
            //todo: check format (32 bytes vs 33 bytes)
            //todo: check key range: from 0x1 to 0xFFFF FFFF FFFF FFFF FFFF FFFF FFFF FFFE BAAE DCE6 AF48 A03B BFD2 5E8C D036 4140 (source https://en.bitcoin.it/wiki/Private_key)

            ECPoint publicKeyPoint = CurveParameters.G.Multiply(new BigInteger(1, privateKey));

            // Compressed and uncopressed formats are defined in "SEC 1: Elliptic Curve Cryptography" in section "2.3.3 Elliptic-Curve-Point-to-Octet-String Conversion".
            // see: http://www.secg.org/sec1-v2.pdf
            ECPoint publicKey = publicKeyPoint.Curve.CreatePoint(publicKeyPoint.X.ToBigInteger(), publicKeyPoint.Y.ToBigInteger(), useCompressedPublicKey);
            byte[] encodedPublicKey = publicKey.GetEncoded();

            //todo: set first byte according to Network type (0x00 for Main Network)
            byte[] addressBytes = new byte[21];
            using (SHA256 sha256Alg = SHA256.Create())
            {
                using (RIPEMD160 ripemd160Alg = RIPEMD160.Create())
                {
                    byte[] ripemd160Hash = ripemd160Alg.ComputeHash(sha256Alg.ComputeHash(encodedPublicKey));
                    Array.Copy(ripemd160Hash, 0, addressBytes, 1, 20);
                }
            }

            return Base58Check.Encode(addressBytes);
        }
    }
}