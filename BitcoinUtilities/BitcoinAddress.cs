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
        /// Creates a bitcoin address for the given network kind from the private key.
        /// </summary>
        /// <param name="networkKind">The kind of the network in which address will be used.</param>
        /// <param name="privateKey">The array of 32 bytes of the private key.</param>
        /// <param name="useCompressedPublicKey">true to specify that the public key should have the compressed format; otherwise, false.</param>
        /// <exception cref="ArgumentException">The private key is invalid.</exception>
        /// <returns>Bitcoin address in Base58Check encoding.</returns>
        public static string FromPrivateKey(BitcoinNetworkKind networkKind, byte[] privateKey, bool useCompressedPublicKey)
        {
            if (!BitcoinPrivateKey.IsValid(privateKey))
            {
                throw new ArgumentException("The private key is invalid.", nameof(privateKey));
            }

            byte[] encodedPublicKey = BitcoinPrivateKey.ToEncodedPublicKey(privateKey, useCompressedPublicKey);

            return FromPublicKey(networkKind, encodedPublicKey);
        }

        /// <summary>
        /// Converts a public key to a bitcoin address for the given network kind.
        /// <para/>
        /// This method does not validate the public key.
        /// </summary>
        /// <param name="networkKind">The kind of the network in which address will be used.</param>
        /// <param name="publicKey">The array of bytes of the public key.</param>
        /// <returns>Bitcoin address in Base58Check encoding; or null if the public key is null.</returns>
        public static string FromPublicKey(BitcoinNetworkKind networkKind, byte[] publicKey)
        {
            if (publicKey == null)
            {
                return null;
            }

            byte[] addressBytes = new byte[21];
            addressBytes[0] = EncodeAddressVersion(networkKind, BitcoinAddressUsage.PayToPublicKeyHash);
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

        public static bool TryDecode(string address, out BitcoinNetworkKind networkKind, out BitcoinAddressUsage addressUsage, out byte[] publicKeyHash)
        {
            // todo: add XMLDOC and tests

            // todo: do we really need so many parameters (networkKind, addressUsage)? Should we just have some network-specific formatters?

            networkKind = default(BitcoinNetworkKind);
            addressUsage = default(BitcoinAddressUsage);
            publicKeyHash = null;

            if (!Base58Check.TryDecode(address, out byte[] decodedBytes))
            {
                return false;
            }

            if (decodedBytes.Length != 21)
            {
                return false;
            }

            if (!TryDecodeAddressVersion(decodedBytes[0], out networkKind, out addressUsage))
            {
                return false;
            }

            publicKeyHash = new byte[decodedBytes.Length - 1];
            Array.Copy(decodedBytes, 1, publicKeyHash, 0, publicKeyHash.Length);

            return true;
        }

        /// <summary>
        /// Specification: https://en.bitcoin.it/wiki/List_of_address_prefixes
        /// </summary>
        private static byte EncodeAddressVersion(BitcoinNetworkKind networkKind, BitcoinAddressUsage addressUsage)
        {
            if (networkKind == BitcoinNetworkKind.Main)
            {
                if (addressUsage == BitcoinAddressUsage.PayToPublicKeyHash)
                {
                    return 0x00;
                }

                if (addressUsage == BitcoinAddressUsage.PayToScriptHash)
                {
                    return 0x05;
                }
            }

            if (networkKind == BitcoinNetworkKind.Test)
            {
                if (addressUsage == BitcoinAddressUsage.PayToPublicKeyHash)
                {
                    return 0x6F;
                }

                if (addressUsage == BitcoinAddressUsage.PayToScriptHash)
                {
                    return 0xC4;
                }
            }

            throw new ArgumentException($"Unexpected network kind / address version: {networkKind} / {addressUsage}.");
        }

        /// <summary>
        /// Specification: https://en.bitcoin.it/wiki/List_of_address_prefixes
        /// </summary>
        private static bool TryDecodeAddressVersion(byte versionByte, out BitcoinNetworkKind networkKind, out BitcoinAddressUsage addressUsage)
        {
            if (versionByte == 0x00)
            {
                networkKind = BitcoinNetworkKind.Main;
                addressUsage = BitcoinAddressUsage.PayToPublicKeyHash;
                return true;
            }

            if (versionByte == 0x05)
            {
                networkKind = BitcoinNetworkKind.Main;
                addressUsage = BitcoinAddressUsage.PayToScriptHash;
                return true;
            }

            if (versionByte == 0x6F)
            {
                networkKind = BitcoinNetworkKind.Test;
                addressUsage = BitcoinAddressUsage.PayToPublicKeyHash;
                return true;
            }

            if (versionByte == 0xC4)
            {
                networkKind = BitcoinNetworkKind.Test;
                addressUsage = BitcoinAddressUsage.PayToScriptHash;
                return true;
            }

            networkKind = default(BitcoinNetworkKind);
            addressUsage = default(BitcoinAddressUsage);
            return false;
        }
    }
}