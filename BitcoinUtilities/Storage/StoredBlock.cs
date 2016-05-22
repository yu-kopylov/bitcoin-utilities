using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Storage
{
    /// <summary>
    /// This class represents a block in a storage.
    /// Besides block header, it contains other information related to storage and the position of the block within blockchain.
    /// </summary>
    public class StoredBlock
    {
        public StoredBlock(BlockHeader header)
        {
            Header = header;

            byte[] text = BitcoinStreamWriter.GetBytes(header.Write);

            Hash = CryptoUtils.DoubleSha256(text);
            Height = -1;
            TotalWork = 0;
            HasContent = false;
        }

        /// <summary>
        /// The block header.
        /// </summary>
        public BlockHeader Header { get; private set; }

        /// <summary>
        /// The hash of the block header.
        /// </summary>
        public byte[] Hash { get; private set; }

        /// <summary>
        /// The height of the block in the blockchain.
        /// <para/>
        /// It equals -1 if the height of the block in unknown.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// An estimated amount of work that was required to build blockchain up to this block.
        /// <para/>
        /// It is measured as number of hashes that had to be generated to build blocks in the blockchain.
        /// </summary>
        public double TotalWork { get; set; }

        /// <summary>
        /// True if storage has the content of the block. 
        /// <para/>
        /// False if only the block header is stored.
        /// </summary>
        public bool HasContent { get; private set; }
    }
}