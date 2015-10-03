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
        private static readonly int[] AlphabetLookup = new int[255];
        private static readonly BigInteger Base58Pow8 = BigInteger.Pow(58, 8);

        static Base58Check()
        {
            for (int i = 0; i < AlphabetLookup.Length; i++)
            {
                AlphabetLookup[i] = -1;
            }
            for (int i = 0; i < Alphabet.Length; i++)
            {
                AlphabetLookup[Alphabet[i]] = i;
            }
        }

        /// <summary>
        /// Encodes the given bytes using the Base58Check encoding.<br/>
        /// Includes the check code.
        /// Assumes that a version byte is already included.
        /// </summary>
        /// <param name="data">The array of bytes to encode including the version byte.</param>
        /// <returns>The result of encoding.</returns>
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
        /// <param name="data">The array of bytes to encode.</param>
        /// <returns>The result of encoding.</returns>
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

        /// <summary>
        /// Decodes the given Base58 string into the array of bytes.<br/>
        /// Does not validate a check code.
        /// </summary>
        /// <param name="str">The string to decode.</param>
        /// <param name="result">The result of decoding.</param>
        /// <returns>true if the given string was converted successfully; otherwise, false.</returns>
        internal static bool TryDecodeNoCheck(string str, out byte[] result)
        {
            int leadingZeroes = 0;
            while (leadingZeroes < str.Length && str[leadingZeroes] == Alphabet[0])
            {
                leadingZeroes++;
            }

            BigInteger value = BigInteger.Zero;
            ulong accumulator = 0;
            int accumulatedChars = 0;
            for (int i = leadingZeroes; i < str.Length; i++)
            {
                char c = str[i];
                int val;
                if (c > 255)
                {
                    result = null;
                    return false;
                }
                val = AlphabetLookup[c];
                if (val < 0)
                {
                    result = null;
                    return false;
                }
                accumulator = accumulator*58 + (ulong) val;
                accumulatedChars++;
                if (accumulatedChars == 8)
                {
                    value = value*Base58Pow8 + accumulator;
                    accumulator = 0;
                    accumulatedChars = 0;
                }
            }

            if (accumulatedChars != 0)
            {
                value = value*BigInteger.Pow(58, accumulatedChars) + accumulator;
            }

            byte[] decodedBytes = value.ToByteArray();
            Array.Reverse(decodedBytes);
            int firstByte = 0;
            while (firstByte < decodedBytes.Length && decodedBytes[firstByte] == 0)
            {
                firstByte++;
            }

            result = new byte[leadingZeroes + decodedBytes.Length - firstByte];
            Array.Copy(decodedBytes, firstByte, result, leadingZeroes, decodedBytes.Length - firstByte);
            return true;
        }
    }
}