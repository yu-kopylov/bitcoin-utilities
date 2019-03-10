namespace BitcoinUtilities
{
    public class BloomFilter
    {
        public BloomFilter(int functionCount, uint tweak, byte[] bits)
        {
            FunctionCount = functionCount;
            Tweak = tweak;
            Bits = bits;
        }

        public int FunctionCount { get; }
        public uint Tweak { get; }
        public byte[] Bits { get; }

        public void Add(byte[] data)
        {
            for (uint i = 0; i < FunctionCount; i++)
            {
                uint hash = Murmur3x86.GetHash32(i * 0xFBA4C795u + Tweak, data);
                int byteOffset = (int) (hash >> 3) % Bits.Length;
                int bitOffset = (int) (hash & 7);
                Bits[byteOffset] |= (byte) (1 << bitOffset);
            }
        }

        public bool Contains(byte[] data)
        {
            for (uint i = 0; i < FunctionCount; i++)
            {
                uint hash = Murmur3x86.GetHash32(i * 0xFBA4C795u + Tweak, data);
                int byteOffset = (int) (hash >> 3) % Bits.Length;
                int bitOffset = (int) (hash & 7);
                if ((Bits[byteOffset] & (1 << bitOffset)) == 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}