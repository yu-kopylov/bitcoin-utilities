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
        public static string Encode(string address, byte[] privateKey, string password, bool useEcMultiplication, bool hasLotAndSequence, bool compressed)
        {
            //todo: check key range: from 0x1 to 0xFFFF FFFF FFFF FFFF FFFF FFFF FFFF FFFE BAAE DCE6 AF48 A03B BFD2 5E8C D036 4140 (source https://en.bitcoin.it/wiki/Private_key)
            //todo: get address from key (see https://en.bitcoin.it/wiki/Technical_background_of_version_1_Bitcoin_addresses)
            //todo: support useEcMultiplication, hasLotAndSequence and compressed modes

            if (useEcMultiplication || hasLotAndSequence || compressed)
            {
                throw new ArgumentException("Only non-EC-multiplied, non-compressed mode is implemented.");
            }

            password = password.Normalize(NormalizationForm.FormC);
            Encoding utf8Encoding = new UTF8Encoding(false);
            byte[] passwordBytes = utf8Encoding.GetBytes(password);

            byte[] result = new byte[39];
            result[0] = 1;
            result[1] = useEcMultiplication ? (byte) 0x43 : (byte) 0x42;

            byte flagByte = 0;
            if (!useEcMultiplication)
            {
                flagByte |= 0x80 | 0x40;
            }
            if (compressed)
            {
                flagByte |= 0x20;
            }
            if (useEcMultiplication && hasLotAndSequence)
            {
                flagByte |= 0x04;
            }
            result[2] = flagByte;

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

            for (int i = 0; i < 32; i++)
            {
                privateKey[i] ^= derivedKey[i];
            }
            using (Aes aesAlg = Aes.Create())
            {
                Contract.Assume(aesAlg != null);
                aesAlg.Mode = CipherMode.ECB;
                using (ICryptoTransform encryptor = aesAlg.CreateEncryptor(derivedHalf2, null))
                {
                    encryptor.TransformBlock(privateKey, 0, 32, result, 7);
                }
            }

            return Base58Check.Encode(result);
        }
    }
}