namespace BitcoinUtilities
{
    public static class Murmur3x86
    {
        public static uint GetHash32(uint nHashSeed, byte[] data)
        {
            int len = data.Length;
            uint h1 = nHashSeed;

            const uint c1 = 0xCC9E2D51;
            const uint c2 = 0x1B873593;

            int ofs = 0;
            for (; ofs + 3 < len; ofs += 4)
            {
                uint k1 = (uint) (data[ofs] | data[ofs + 1] << 8 | data[ofs + 2] << 16 | data[ofs + 3] << 24);

                k1 *= c1;
                k1 = RotateLeft32(k1, 15);
                k1 *= c2;

                h1 ^= k1;
                h1 = RotateLeft32(h1, 13);
                h1 = h1 * 5 + 0xE6546B64;
            }

            if (ofs < len)
            {
                uint k1 = 0;

                for (int tailOfs = len - 1; tailOfs >= ofs; tailOfs--)
                {
                    k1 = k1 << 8 | data[tailOfs];
                }

                k1 *= c1;
                k1 = RotateLeft32(k1, 15);
                k1 *= c2;
                h1 ^= k1;
            }

            h1 ^= (uint) len;
            h1 ^= h1 >> 16;
            h1 *= 0x85ebca6b;
            h1 ^= h1 >> 13;
            h1 *= 0xc2b2ae35;
            h1 ^= h1 >> 16;

            return h1;
        }

        private static uint RotateLeft32(uint value, int bits)
        {
            return (value << bits) | (value >> (32 - bits));
        }
    }
}