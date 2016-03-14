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

        /// <summary>
        /// Encodes the private key for Main Network in the WIF format.
        /// </summary>
        /// <param name="privateKey">The array of 32 bytes of the private key.</param>
        /// <param name="useCompressedPublicKey">true to specify that the public key should have the compressed format; otherwise, false.</param>
        /// <exception cref="ArgumentException">The private key is invalid.</exception>
        /// <returns>The private key encoded in the WIF format.</returns>
        public static string Encode(byte[] privateKey, bool useCompressedPublicKey)
        {
            if (!BitcoinPrivateKey.IsValid(privateKey))
            {
                throw new ArgumentException("The private key is invalid.", "privateKey");
            }

            byte[] wifBytes;
            if (useCompressedPublicKey)
            {
                wifBytes = new byte[CompressedWifLength];
                wifBytes[CompressedWifLength - 1] = 0x01;
            }
            else
            {
                wifBytes = new byte[UncompressedWifLength];
            }

            //todo: set first byte according to Network type (0x80 for Main Network, 0xEF for Test Network)
            wifBytes[0] = 0x80;
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
        public static bool Decode(string wif, out byte[] privateKey, out bool useCompressedPublicKey)
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
                if (wifBytes[CompressedWifLength - 1] != 0x01)
                {
                    return false;
                }
            }

            //todo: support Test Network first byte according to Network type (0x80 for Main Network, 0xEF for Test Network)
            if (wifBytes[0] != 0x80)
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
    }
}