using System;

namespace BitcoinUtilities
{
    public static class HexUtils
    {
        /// <summary>
        /// Converts a byte array to a hexadecimal string.
        /// </summary>
        /// <param name="array">The byte array to convert.</param>
        /// <returns>A hexadecimal string converted from the given array.</returns>
        /// <exception cref="ArgumentNullException">If the given array is null.</exception>
        public static string GetString(byte[] array)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array), "The given array is null.");
            }

            char[] octets = new char[array.Length * 2];
            int ofs = 0;
            foreach (byte b in array)
            {
                octets[ofs] = GetOctet(b >> 4);
                octets[ofs + 1] = GetOctet(b & 0xF);
                ofs += 2;
            }

            return new string(octets);
        }

        /// <summary>
        /// Converts a byte array to a hexadecimal string using reverse byte order.
        /// </summary>
        /// <param name="array">The byte array to convert.</param>
        /// <returns>A hexadecimal string converted from the given array.</returns>
        /// <exception cref="ArgumentNullException">If the given array is null.</exception>
        public static string GetReversedString(byte[] array)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array), "The given array is null.");
            }

            char[] octets = new char[array.Length * 2];
            int ofs = octets.Length - 2;
            foreach (byte b in array)
            {
                octets[ofs] = GetOctet(b >> 4);
                octets[ofs + 1] = GetOctet(b & 0xF);
                ofs -= 2;
            }

            return new string(octets);
        }

        /// <summary>
        /// Converts a hexadecimal string to a byte array.
        /// </summary>
        /// <param name="hex">The string to convert. The only allowed characters are 0-9, a-z, A-Z.</param>
        /// <returns>A byte array converted from the given string.</returns>
        /// <exception cref="ArgumentNullException">If the given string is not a valid hexadecimal string, or is null.</exception>
        public static byte[] GetBytesUnsafe(string hex)
        {
            if (!TryGetBytes(hex, out var result))
            {
                throw new ArgumentException("Invalid hexadecimal string.", nameof(hex));
            }

            return result;
        }

        /// <summary>
        /// Converts a hexadecimal string to a byte array.
        /// </summary>
        /// <param name="hex">The string to convert. The only allowed characters are 0-9, a-z, A-Z.</param>
        /// <param name="result">A byte array converted from the given string; or null if conversion failed.</param>
        /// <returns>true if string was converted to bytes; otherwise, false.</returns>
        public static bool TryGetBytes(string hex, out byte[] result)
        {
            result = null;

            if (hex == null || hex.Length % 2 != 0)
            {
                return false;
            }

            int arrayLength = hex.Length / 2;
            byte[] array = new byte[arrayLength];

            for (int i = 0, ofs = 0; i < arrayLength; i++, ofs += 2)
            {
                byte hi, low;
                if (!TryParseOctet(hex[ofs], out hi) || !TryParseOctet(hex[ofs + 1], out low))
                {
                    return false;
                }

                array[i] = (byte) (hi << 4 | low);
            }

            result = array;
            return true;
        }

        /// <summary>
        /// This method does not validate parameters. It is responsibility of the caller to pass values in range from 0 to 15.
        /// </summary>
        private static char GetOctet(int octetValue)
        {
            if (octetValue < 10)
            {
                return (char) ('0' + octetValue);
            }

            return (char) ('a' + octetValue - 10);
        }

        private static bool TryParseOctet(char octet, out byte octetValue)
        {
            octetValue = 0;

            if (octet >= '0' && octet <= '9')
            {
                octetValue = (byte) (octet - '0');
                return true;
            }

            if (octet >= 'a' && octet <= 'f')
            {
                octetValue = (byte) (octet - 'a' + 10);
                return true;
            }

            if (octet >= 'A' && octet <= 'F')
            {
                octetValue = (byte) (octet - 'A' + 10);
                return true;
            }

            return false;
        }
    }
}