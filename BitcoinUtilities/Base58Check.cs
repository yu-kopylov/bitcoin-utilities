using System;
using System.Numerics;
using System.Security.Cryptography;

namespace BitcoinUtilities
{
    /// <summary>
    /// Specification: https://en.bitcoin.it/wiki/Base58Check_encoding
    /// </summary>
    public static class Base58Check
    {
        private const string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        private static readonly BigInteger Base58Pow8 = BigInteger.Pow(58, 8);

        /// <summary>
        /// Encodes the given bytes using the Base58Check encoding.<br/>
        /// Includes a check code.
        /// Assumes that a version byte is already included.
        /// </summary>
        /// <param name="data">Bytes to encode including version byte.</param>
        /// <returns>Results of encodung.</returns>
        public static string Encode(byte[] data)
        {
            byte[] buf = new byte[data.Length + 4];
            Array.Copy(data, 0, buf, 0, data.Length);
            using (SHA256 sha256Alg = SHA256.Create())
            {
                byte[] hash = sha256Alg.ComputeHash(sha256Alg.ComputeHash(data));
                Array.Copy(hash, 0, buf, data.Length, 4);
            }

            return EncodeNoCheck(buf);
        }

        /// <summary>
        /// Encodes the given bytes using the Base58Check encoding.<br/>
        /// Does not include a check code.
        /// </summary>
        /// <param name="data">Bytes to encode.</param>
        /// <returns>Results of encodung.</returns>
        internal static string EncodeNoCheck(byte[] data)
        {
            byte[] unsignedData = new byte[data.Length + 1];
            Array.Copy(data, 0, unsignedData, 0, data.Length);
            Array.Reverse(unsignedData, 0, data.Length);
            BigInteger value = new BigInteger(unsignedData);

            int maxResultLength = 1 + data.Length*7/5; // Log(256, 58) = 1.365658237
            char[] res = new char[maxResultLength];
            int firstCharOfs = maxResultLength;

            while (value != 0)
            {
                BigInteger bigReminder;
                value = BigInteger.DivRem(value, Base58Pow8, out bigReminder);
                ulong reminder = (ulong) bigReminder;
                res[--firstCharOfs] = Alphabet[(int) (reminder%58)];
                reminder /= 58;
                for (int i = 0; reminder != 0 || (!value.IsZero && i < 7); i++)
                {
                    res[--firstCharOfs] = Alphabet[(int) (reminder%58)];
                    reminder /= 58;
                }
            }

            for (int leadingZeros = 0; leadingZeros < data.Length && data[leadingZeros] == 0; leadingZeros++)
            {
                --firstCharOfs;
                res[firstCharOfs] = Alphabet[0];
            }

            return new string(res, firstCharOfs, maxResultLength - firstCharOfs);
        }
    }
}