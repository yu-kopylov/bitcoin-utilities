using System;

namespace BitcoinUtilities
{
    /// <summary>
    /// Specification: https://en.bitcoin.it/wiki/Wallet_import_format
    /// </summary>
    public static class Wif
    {
        private const int UncompressedWifLength = 33;
        private const int CompressedWifLength = 34;
        private const byte CompressedPublicKeyFlag = 0x01;

        /// <summary>
        /// Encodes the private key for the given network in the WIF format.
        /// </summary>
        /// <param name="privateKey">The array of 32 bytes of the private key.</param>
        /// <param name="useCompressedPublicKey">true to specify that the public key should have the compressed format; otherwise, false.</param>
        /// <param name="networkKind">The kind of the network for which key is encoded.</param>
        /// <returns>The private key encoded in the WIF format.</returns>
        /// <exception cref="ArgumentException">The private key is invalid.</exception>
        public static string Encode(BitcoinNetworkKind networkKind, byte[] privateKey, bool useCompressedPublicKey)
        {
            if (!BitcoinPrivateKey.IsValid(privateKey))
            {
                throw new ArgumentException("The private key is invalid.", "privateKey");
            }

            byte[] wifBytes;
            if (useCompressedPublicKey)
            {
                wifBytes = new byte[CompressedWifLength];
                wifBytes[CompressedWifLength - 1] = CompressedPublicKeyFlag;
            }
            else
            {
                wifBytes = new byte[UncompressedWifLength];
            }

            wifBytes[0] = GetAddressVersion(networkKind);
            Array.Copy(privateKey, 0, wifBytes, 1, BitcoinPrivateKey.KeyLength);

            return Base58Check.Encode(wifBytes);
        }

        /// <summary>
        /// Decodes the private key for Main Network in the WIF format.
        /// </summary>
        /// <param name="wif">The private key in the WIF format.</param>
        /// <param name="privateKey">If the key was decoded successfully, the array of 32 bytes of the private key; otherwise, null.</param>
        /// <param name="useCompressedPublicKey">true if the key was decoded successfully and the public key should have the compressed format; otherwise, false.</param>
        /// <returns>true if the key was decoded successfully; otherwise, false.</returns>
        public static bool TryDecode(BitcoinNetworkKind networkKind, string wif, out byte[] privateKey, out bool useCompressedPublicKey)
        {
            privateKey = null;
            useCompressedPublicKey = false;

            if (wif == null)
            {
                return false;
            }

            byte[] wifBytes;
            if (!Base58Check.TryDecode(wif, out wifBytes))
            {
                return false;
            }

            if (wifBytes.Length != UncompressedWifLength && wifBytes.Length != CompressedWifLength)
            {
                return false;
            }

            useCompressedPublicKey = wifBytes.Length == CompressedWifLength;
            if (useCompressedPublicKey)
            {
                if (wifBytes[CompressedWifLength - 1] != CompressedPublicKeyFlag)
                {
                    return false;
                }
            }

            if (wifBytes[0] != GetAddressVersion(networkKind))
            {
                return false;
            }

            privateKey = new byte[BitcoinPrivateKey.KeyLength];
            Array.Copy(wifBytes, 1, privateKey, 0, BitcoinPrivateKey.KeyLength);

            if (!BitcoinPrivateKey.IsValid(privateKey))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Specification: https://en.bitcoin.it/wiki/List_of_address_prefixes
        /// </summary>
        private static byte GetAddressVersion(BitcoinNetworkKind networkKind)
        {
            if (networkKind == BitcoinNetworkKind.Main)
            {
                return 0x80;
            }
            if (networkKind == BitcoinNetworkKind.Test)
            {
                return 0xEF;
            }
            throw new ArgumentException($"Unexpected network kind: {networkKind}", nameof(networkKind));
        }
    }
}