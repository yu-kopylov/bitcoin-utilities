using System.Diagnostics.Contracts;

namespace BitcoinUtilities.Collections.VirtualDictionaryInternals
{
    internal struct ByteArrayRef
    {
        private readonly byte[] array;
        private readonly int position;
        private readonly int length;

        public ByteArrayRef(byte[] array, int position, int length)
        {
            this.array = array;
            this.position = position;
            this.length = length;
        }

        public byte[] Array
        {
            get { return array; }
        }

        public int Position
        {
            get { return position; }
        }

        public int Length
        {
            get { return length; }
        }

        public byte GetByteAt(int ofs)
        {
            Contract.Assert(ofs >= 0);
            Contract.Assert(ofs < length);
            return array[position + ofs];
        }

        public byte[] ToArray()
        {
            byte[] res = new byte[length];
            System.Array.Copy(array, position, res, 0, length);
            return res;
        }
    }
}