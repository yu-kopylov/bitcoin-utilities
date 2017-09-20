using System;
using System.Security.Cryptography;

namespace BitcoinUtilities
{
    /// <summary>
    /// Specification: https://en.bitcoin.it/wiki/Technical_background_of_version_1_Bitcoin_addresses
    /// </summary>
    public static class BitcoinAddress
    {
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
                throw new ArgumentException("The private key is invalid.", nameof(privateKey));
            }

            byte[] encodedPublicKey = BitcoinPrivateKey.ToEncodedPublicKey(privateKey, useCompressedPublicKey);

            return FromPublicKey(encodedPublicKey, BitcoinNetworkKind.Main);
        }

        /// <summary>
        /// Converts a public key to a bitcoin address for the given network type.
        /// <para/>
        /// This method does not validate the public key.
        /// </summary>
        /// <param name="publicKey">The array of bytes of the public key.</param>
        /// <param name="networkKind">The kind of the network in which address will be used.</param>
        /// <returns>Bitcoin address in Base58Check encoding; or null if the public key is null.</returns>
        public static string FromPublicKey(byte[] publicKey, BitcoinNetworkKind networkKind)
        {
            if (publicKey == null)
            {
                return null;
            }

            byte[] addressBytes = new byte[21];
            addressBytes[0] = GetAddressVersion(networkKind);
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

        /// <summary>
        /// Specification: https://en.bitcoin.it/wiki/List_of_address_prefixes
        /// </summary>
        private static byte GetAddressVersion(BitcoinNetworkKind networkKind)
        {
            if (networkKind == BitcoinNetworkKind.Main)
            {
                return 0x00;
            }
            if (networkKind == BitcoinNetworkKind.Test)
            {
                return 0x6F;
            }
            throw new ArgumentException($"Unexpected network kind: {networkKind}", nameof(networkKind));
        }
    }
}