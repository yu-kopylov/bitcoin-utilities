using System;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Text;
using CryptSharp.Utility;

namespace BitcoinUtilities
{
    /// <summary>
    /// Specification: https://github.com/bitcoin/bips/blob/master/bip-0038.mediawiki
    /// </summary>
    public static class Bip38
    {
        private const int EncryptedKeyLength = 39;

        private const byte PrefixNonEc = 0x42;
        private const byte PrefixEc = 0x43;

        private const byte FlagNonEc = 0x80 | 0x40;
        private const byte FlagCompressed = 0x20;
        private const byte FlagHasLotAndSequence = 0x04;

        /// <summary>
        /// Encrypts the private key with the given password.
        /// </summary>
        /// <param name="privateKey">The array of 32 bytes of the private key.</param>
        /// <param name="password">The password.</param>
        /// <param name="useCompressedPublicKey">true to specify that the public key should have the compressed format; otherwise, false.</param>
        /// <exception cref="ArgumentException">The private key is invalid or the password is null.</exception>
        /// <returns>The encrypted private key in Base58Check encoding.</returns>
        public static string Encrypt(byte[] privateKey, string password, bool useCompressedPublicKey)
        {
            if (!BitcoinPrivateKey.IsValid(privateKey))
            {
                throw new ArgumentException("The private key is invalid.", nameof(privateKey));
            }

            if (password == null)
            {
                throw new ArgumentException("The password is null.", nameof(privateKey));
            }

            password = password.Normalize(NormalizationForm.FormC);
            Encoding utf8Encoding = new UTF8Encoding(false);
            byte[] passwordBytes = utf8Encoding.GetBytes(password);

            byte[] result = new byte[EncryptedKeyLength];
            result[0] = 1;
            result[1] = PrefixNonEc;

            byte flagByte = FlagNonEc;
            if (useCompressedPublicKey)
            {
                flagByte |= FlagCompressed;
            }
            result[2] = flagByte;

            string address = BitcoinAddress.FromPrivateKey(privateKey, useCompressedPublicKey);
            byte[] addressBytes = Encoding.ASCII.GetBytes(address);

            byte[] addressHash = new byte[4];
            using (SHA256 sha256Alg = SHA256.Create())
            {
                byte[] addressHashFull = sha256Alg.ComputeHash(sha256Alg.ComputeHash(addressBytes));
                Array.Copy(addressHashFull, 0, addressHash, 0, 4);
                Array.Copy(addressHashFull, 0, result, 3, 4);
            }

            byte[] derivedKey = SCrypt.ComputeDerivedKey(passwordBytes, addressHash, 16384, 8, 8, null, 64);
            byte[] derivedHalf2 = new byte[32];
            Array.Copy(derivedKey, 32, derivedHalf2, 0, 32);

            byte[] maskedPrivateKey = new byte[32];
            Array.Copy(privateKey, 0, maskedPrivateKey, 0, 32);
            for (int i = 0; i < 32; i++)
            {
                maskedPrivateKey[i] ^= derivedKey[i];
            }

            using (Aes aesAlg = Aes.Create())
            {
                Contract.Assume(aesAlg != null);
                aesAlg.Mode = CipherMode.ECB;
                using (ICryptoTransform aesTransform = aesAlg.CreateEncryptor(derivedHalf2, null))
                {
                    int ofs = 0;
                    int rest = 32;
                    while (rest > 0)
                    {
                        int bytesTransformed = aesTransform.TransformBlock(maskedPrivateKey, ofs, rest, result, 7 + ofs);
                        rest -= bytesTransformed;
                        ofs += bytesTransformed;
                    }
                }
            }

            return Base58Check.Encode(result);
        }

        /// <summary>
        /// Decrypts the given Base58-string using the given password.
        /// </summary>
        /// <param name="encryptedKey">The encrypted Base58 string.</param>
        /// <param name="password">The password.</param>
        /// <param name="privateKey">The decrypted private key.</param>
        /// <param name="useCompressedPublicKey">true to specify that the public key should have the compressed format; otherwise, false.</param>
        /// <returns>true if the given string was decrypted successfully; otherwise, false.</returns>
        public static bool TryDecrypt(string encryptedKey, string password, out byte[] privateKey, out bool useCompressedPublicKey)
        {
            privateKey = null;
            useCompressedPublicKey = false;

            byte[] encryptedKeyBytes;
            if (!Base58Check.TryDecode(encryptedKey, out encryptedKeyBytes))
            {
                return false;
            }

            if (password == null)
            {
                throw new ArgumentException("The password is null.", nameof(privateKey));
            }

            if (!ValidateEncryptedKeyStructure(encryptedKeyBytes))
            {
                return false;
            }

            if (encryptedKeyBytes[1] != PrefixNonEc)
            {
                return false;
            }

            useCompressedPublicKey = (encryptedKeyBytes[2] & FlagCompressed) == FlagCompressed;

            password = password.Normalize(NormalizationForm.FormC);
            Encoding utf8Encoding = new UTF8Encoding(false);
            byte[] passwordBytes = utf8Encoding.GetBytes(password);
            byte[] addressHash = new byte[4];
            Array.Copy(encryptedKeyBytes, 3, addressHash, 0, 4);

            byte[] derivedKey = SCrypt.ComputeDerivedKey(passwordBytes, addressHash, 16384, 8, 8, null, 64);
            byte[] derivedHalf2 = new byte[32];
            Array.Copy(derivedKey, 32, derivedHalf2, 0, 32);

            byte[] result = new byte[32];
            using (Aes aesAlg = Aes.Create())
            {
                Contract.Assume(aesAlg != null);
                aesAlg.Mode = CipherMode.ECB;
                using (ICryptoTransform aesTransform = aesAlg.CreateDecryptor(derivedHalf2, null))
                {
                    int ofs = 0;
                    int rest = 32;
                    while (rest > 0)
                    {
                        int bytesTransformed = aesTransform.TransformBlock(encryptedKeyBytes, 7 + ofs, rest, result, ofs);
                        rest -= bytesTransformed;
                        ofs += bytesTransformed;
                    }
                }
            }

            for (int i = 0; i < 32; i++)
            {
                result[i] ^= derivedKey[i];
            }

            if (!BitcoinPrivateKey.IsValid(result))
            {
                return false;
            }

            string address = BitcoinAddress.FromPrivateKey(result, useCompressedPublicKey);
            byte[] addressBytes = Encoding.ASCII.GetBytes(address);

            using (SHA256 sha256Alg = SHA256.Create())
            {
                byte[] addressHashFull = sha256Alg.ComputeHash(sha256Alg.ComputeHash(addressBytes));
                for (int i = 0; i < 4; i++)
                {
                    if (addressHash[i] != addressHashFull[i])
                    {
                        return false;
                    }
                }
            }

            privateKey = result;
            return true;
        }

        /// <summary>
        /// Validates that encrypted private key has valid structure.
        /// Checks length, prefix and flags of the private key.
        /// </summary>
        /// <param name="encryptedKey">The array of byted to validate as encrypted private key.</param>
        /// <returns>true if the given byte array has correct BIP-38 structure; otherwise, false.</returns>
        private static bool ValidateEncryptedKeyStructure(byte[] encryptedKey)
        {
            if (encryptedKey.Length != EncryptedKeyLength)
            {
                return false;
            }

            if (encryptedKey[0] != 1)
            {
                return false;
            }

            byte flagByte = encryptedKey[2];
            if (encryptedKey[1] == PrefixNonEc)
            {
                if ((flagByte & FlagNonEc) != FlagNonEc)
                {
                    return false;
                }
                if ((flagByte & ~(FlagNonEc | FlagCompressed)) != 0)
                {
                    return false;
                }
            }
            else if (encryptedKey[1] == PrefixEc)
            {
                if ((flagByte & FlagNonEc) != 0)
                {
                    return false;
                }
                if ((flagByte & ~(FlagNonEc | FlagCompressed | FlagHasLotAndSequence)) != 0)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}