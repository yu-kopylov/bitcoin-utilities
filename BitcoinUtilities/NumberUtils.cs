using System;
using System.Numerics;

namespace BitcoinUtilities
{
    public static class NumberUtils
    {
        /// <summary>
        /// Constructs a 2-byte unsigned integer with a reversed order of bytes in its binary representation.
        /// </summary>
        public static ushort ReverseBytes(ushort value)
        {
            value = (ushort) (value << 8 | value >> 8);
            return value;
        }

        /// <summary>
        /// Constructs a 4-byte unsigned integer with a reversed order of bytes in its binary representation.
        /// </summary>
        public static uint ReverseBytes(uint value)
        {
            value = value << 16 | value >> 16;
            value = (value & 0xFF00FF00u) >> 8 | (value & 0x00FF00FFu) << 8;
            return value;
        }

        /// <summary>
        /// Converts the specified 32-bit integer value to an array of bytes, using the little-endian encoding.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>An array of 4 bytes.</returns>
        public static byte[] GetBytes(int value)
        {
            byte[] res = new byte[4];
            res[0] = (byte) value;
            res[1] = (byte) (value >> 8);
            res[2] = (byte) (value >> 16);
            res[3] = (byte) (value >> 24);
            return res;
        }

        /// <summary>
        /// Converts a byte array to an unsigned <see cref="BigInteger"/>.
        /// </summary>
        /// <param name="bytes">The array of bytes to convert in the little-endian byte order.</param>
        /// <returns>A <see cref="BigInteger"/> that corresponds to the given array.</returns>
        public static BigInteger ToBigInteger(byte[] bytes)
        {
            byte[] unsignedBytes = new byte[bytes.Length+1];
            Array.Copy(bytes, unsignedBytes, bytes.Length); 
            //todo: test and specify byte order
            return new BigInteger(unsignedBytes);
        }

        /// <summary>
        /// Converts a nBits to a target threshold.
        /// </summary>
        /// <remark>Specification: https://bitcoin.org/en/developer-reference#target-nbits</remark>
        /// <param name="nBits">The nBits to convert.</param>
        /// <returns>The target threshold as a <see cref="BigInteger"/>.</returns>
        public static BigInteger NBitsToTarget(uint nBits)
        {
            int exp = (int) (nBits >> 24) - 3;
            int mantissa = (int) (nBits & 0x007FFFFF);

            //todo: this negative number encoding is non-standard but matches the reference example
            if ((nBits & 0x00800000) != 0)
            {
                mantissa = -mantissa;
            }

            BigInteger res;

            if (exp >= 0)
            {
                res = mantissa*BigInteger.Pow(256, exp);
            }
            else
            {
                res = mantissa/BigInteger.Pow(256, -exp);
            }

            return res;
        }
    }
}