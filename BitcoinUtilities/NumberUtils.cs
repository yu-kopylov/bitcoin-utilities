namespace BitcoinUtilities
{
    public static class NumberUtils
    {
        /// <summary>
        /// Constructs a 4-byte unsigned integer with reverse a order of bytes in its binary representation.
        /// </summary>
        public static uint ReverseBytes(uint value)
        {
            value = value << 16 | value >> 16;
            value = (value & 0xFF00FF00u) >> 8 | (value & 0x00FF00FFu) << 8;
            return value;
        }
    }
}