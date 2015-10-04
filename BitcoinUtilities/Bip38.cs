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
        private const byte FlagNonEc = 0x80 | 0x40;
        private const byte FlagCompressed = 0x20;
        private const byte FlagHasLotAndSequence = 0x04;

        public static string Encrypt(byte[] privateKey, string password, bool compressed)
        {
            //todo: check key range: from 0x1 to 0xFFFF FFFF FFFF FFFF FFFF FFFF FFFF FFFE BAAE DCE6 AF48 A03B BFD2 5E8C D036 4140 (source https://en.bitcoin.it/wiki/Private_key)
            //todo: support compressed modes

            if (privateKey.Length != 32)
            {
                throw new ArgumentException("Thr private key should have length of 32 bytes.", "privateKey");
            }

            if (compressed)
            {
                throw new ArgumentException("Compresed addresses are not implemented yet.");
            }

            password = password.Normalize(NormalizationForm.FormC);
            Encoding utf8Encoding = new UTF8Encoding(false);
            byte[] passwordBytes = utf8Encoding.GetBytes(password);

            byte[] result = new byte[39];
            result[0] = 1;
            result[1] = 0x42;

            byte flagByte = FlagNonEc;
            if (compressed)
            {
                flagByte |= FlagCompressed;
            }
            result[2] = flagByte;

            string address = BitcoinAddress.FromPrivateKey(privateKey);
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

        public static bool TryDecrypt(string encryptedKey, string password, out byte[] privateKey)
        {
            byte[] encryptedKeyBytes;
            if (!Base58Check.TryDecode(encryptedKey, out encryptedKeyBytes))
            {
                privateKey = null;
                return false;
            }

            if (!ValidateEncryptedKeyStructure(encryptedKeyBytes))
            {
                privateKey = null;
                return false;
            }

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

            string address = BitcoinAddress.FromPrivateKey(result);
            byte[] addressBytes = Encoding.ASCII.GetBytes(address);

            using (SHA256 sha256Alg = SHA256.Create())
            {
                byte[] addressHashFull = sha256Alg.ComputeHash(sha256Alg.ComputeHash(addressBytes));
                for (int i = 0; i < 4; i++)
                {
                    if (addressHash[i] != addressHashFull[i])
                    {
                        privateKey = null;
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
            if (encryptedKey.Length != 39)
            {
                return false;
            }

            if (encryptedKey[0] != 1)
            {
                return false;
            }

            byte flagByte = encryptedKey[2];
            if (encryptedKey[1] == 0x42)
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
            else if (encryptedKey[1] == 0x43)
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