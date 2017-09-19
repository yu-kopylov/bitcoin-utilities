using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Storage
{
    /// <summary>
    /// This immutable class represents a block in a storage.
    /// Besides block header, it contains other information related to storage and the position of the block within blockchain.
    /// </summary>
    public class StoredBlock
    {
        internal StoredBlock(BlockHeader header, byte[] hash, int height, double totalWork, bool hasContent, bool isInBestHeaderChain, bool isInBestBlockChain)
        {
            Header = header;
            Hash = hash;
            Height = height;
            TotalWork = totalWork;
            HasContent = hasContent;
            IsInBestHeaderChain = isInBestHeaderChain;
            IsInBestBlockChain = isInBestBlockChain;
        }

        /// <summary>
        /// The block header.
        /// </summary>
        public BlockHeader Header { get; }

        /// <summary>
        /// The hash of the block header.
        /// </summary>
        public byte[] Hash { get; }

        /// <summary>
        /// The height of the block in the blockchain.
        /// <para/>
        /// It equals -1 if the height of the block in unknown.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// An estimated amount of work that was required to build blockchain up to this block.
        /// <para/>
        /// It is measured as number of hashes that had to be generated to build blocks in the blockchain.
        /// </summary>
        public double TotalWork { get; }

        /// <summary>
        /// True if storage has the content of the block. 
        /// <para/>
        /// False if only the block header is stored.
        /// </summary>
        public bool HasContent { get; }

        /// <summary>
        /// True if the block belongs to the chain with the maximum total work; otherwise false.
        /// <para/>
        /// Chain with the maximum total work may not match the current blockchain if blocks in that chain are not yet validated.
        /// </summary>
        public bool IsInBestHeaderChain { get; }

        /// <summary>
        /// True if the block belongs to current blockchain; otherwise false.
        /// </summary>
        public bool IsInBestBlockChain { get; }

        /// <summary>
        /// Returns a copy of this block with the <see cref="HasContent"/> flag set. 
        /// </summary>
        /// <returns></returns>
        public StoredBlock AddContent()
        {
            // todo: some fields are updated through StoredBlock methods and some are updated manually (IsInBestHeaderChain, IsInBestBlockChain)
            StoredBlockBuilder builder = new StoredBlockBuilder(this);
            builder.HasContent = true;
            return builder.Build();
        }

        /// <summary>
        /// Returns a copy of this block with the <see cref="Height"/> and <see cref="TotalWork"/> properties based on the given parent block.
        /// </summary>
        /// <param name="parentBlock">The parent block.</param>
        public StoredBlock AppendAfter(StoredBlock parentBlock)
        {
            double blockWork = DifficultyUtils.DifficultyTargetToWork(DifficultyUtils.NBitsToTarget(Header.NBits));

            StoredBlockBuilder builder = new StoredBlockBuilder(this);
            builder.Height = parentBlock.Height + 1;
            builder.TotalWork = parentBlock.TotalWork + blockWork;

            return builder.Build();
        }
    }

    /// <summary>
    /// This is a builder for the <see cref="StoredBlock"/> class.
    /// </summary>
    public class StoredBlockBuilder
    {
        public StoredBlockBuilder()
        {
            Header = default(BlockHeader);
            Hash = null;
            Height = -1;
            TotalWork = 0;
            HasContent = false;
            IsInBestHeaderChain = false;
            IsInBestBlockChain = false;
        }

        public StoredBlockBuilder(BlockHeader header)
        {
            byte[] text = BitcoinStreamWriter.GetBytes(header.Write);

            Header = header;
            Hash = CryptoUtils.DoubleSha256(text);
            Height = -1;
            TotalWork = 0;
            HasContent = false;
            IsInBestHeaderChain = false;
            IsInBestBlockChain = false;
        }

        public StoredBlockBuilder(StoredBlock block)
        {
            Header = block.Header;
            Hash = block.Hash;
            Height = block.Height;
            TotalWork = block.TotalWork;
            HasContent = block.HasContent;
            IsInBestHeaderChain = block.IsInBestHeaderChain;
            IsInBestBlockChain = block.IsInBestBlockChain;
        }

        /// <summary>
        /// Creates new <see cref="StoredBlock"/> instance with the current properties.
        /// </summary>
        public StoredBlock Build()
        {
            return new StoredBlock(
                Header,
                Hash,
                Height,
                TotalWork,
                HasContent,
                IsInBestHeaderChain,
                IsInBestBlockChain);
        }

        /// <summary>
        /// The block header.
        /// </summary>
        public BlockHeader Header { get; set; }

        /// <summary>
        /// The hash of the block header.
        /// </summary>
        public byte[] Hash { get; set; }

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
        public bool HasContent { get; set; }

        /// <summary>
        /// True if the block belongs to the chain with the maximum total work; otherwise false.
        /// <para/>
        /// Chain with the maximum total work may not match the current blockchain if blocks in that chain are not yet validated.
        /// </summary>
        public bool IsInBestHeaderChain { get; set; }

        /// <summary>
        /// True if the block belongs to current blockchain; otherwise false.
        /// </summary>
        public bool IsInBestBlockChain { get; set; }
    }
}