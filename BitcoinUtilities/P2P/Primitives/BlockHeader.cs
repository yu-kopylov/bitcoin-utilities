namespace BitcoinUtilities.P2P.Primitives
{
    /// <summary>
    /// An 80-byte header belonging to a single block which is hashed repeatedly to create proof of work.
    /// </summary>
    public struct BlockHeader
    {
        private readonly uint version;
        private readonly byte[] prevBlock;
        private readonly byte[] merkleRoot;
        private readonly uint timestamp;
        private readonly uint nBits;
        private readonly uint nonce;

        public BlockHeader(uint version, byte[] prevBlock, byte[] merkleRoot, uint timestamp, uint nBits, uint nonce)
        {
            this.version = version;
            this.prevBlock = prevBlock;
            this.merkleRoot = merkleRoot;
            this.timestamp = timestamp;
            this.nBits = nBits;
            this.nonce = nonce;
        }

        /// <summary>
        /// Block version information, based upon the software version creating this block.
        /// </summary>
        public uint Version
        {
            get { return version; }
        }

        /// <summary>
        /// The hash value of the previous block this particular block references.
        /// </summary>
        public byte[] PrevBlock
        {
            get { return prevBlock; }
        }

        /// <summary>
        /// The reference to a Merkle tree collection which is a hash of all transactions related to this block.
        /// </summary>
        public byte[] MerkleRoot
        {
            get { return merkleRoot; }
        }

        /// <summary>
        /// A timestamp recording when this block was created (limited to 2106).
        /// </summary>
        public uint Timestamp
        {
            get { return timestamp; }
        }

        /// <summary>
        /// The calculated difficulty target being used for this block.
        /// </summary>
        public uint NBits
        {
            get { return nBits; }
        }

        /// <summary>
        /// The nonce used to generate this block to allow variations of the header and compute different hashes.
        /// </summary>
        public uint Nonce
        {
            get { return nonce; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
            writer.Write(version);
            writer.Write(prevBlock);
            writer.Write(merkleRoot);
            writer.Write(timestamp);
            writer.Write(nBits);
            writer.Write(nonce);
        }

        public static BlockHeader Read(BitcoinStreamReader reader)
        {
            uint version = reader.ReadUInt32();
            byte[] prevBlock = reader.ReadBytes(32);
            byte[] merkleRoot = reader.ReadBytes(32);
            uint timestamp = reader.ReadUInt32();
            uint bits = reader.ReadUInt32();
            uint nonce = reader.ReadUInt32();

            return new BlockHeader(version, prevBlock, merkleRoot, timestamp, bits, nonce);
        }
    }
}