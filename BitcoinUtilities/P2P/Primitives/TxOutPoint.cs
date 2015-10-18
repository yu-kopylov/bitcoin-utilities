namespace BitcoinUtilities.P2P.Primitives
{
    /// <summary>
    /// An output transaction reference.
    /// </summary>
    public struct TxOutPoint
    {
        private readonly byte[] hash;
        private readonly uint index;

        public TxOutPoint(byte[] hash, uint index)
        {
            this.hash = hash;
            this.index = index;
        }

        /// <summary>
        /// The hash of the referenced transaction.
        /// </summary>
        public byte[] Hash
        {
            get { return hash; }
        }

        /// <summary>
        /// The index of the specific output in the transaction. The first output is 0, etc.
        /// </summary>
        public uint Index
        {
            get { return index; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
            writer.Write(hash);
            writer.Write(index);
        }

        public static TxOutPoint Read(BitcoinStreamReader reader)
        {
            byte[] hash = reader.ReadBytes(32);
            uint index = reader.ReadUInt32();
            return new TxOutPoint(hash, index);
        }
    }
}