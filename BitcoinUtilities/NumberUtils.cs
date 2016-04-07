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
    }
}