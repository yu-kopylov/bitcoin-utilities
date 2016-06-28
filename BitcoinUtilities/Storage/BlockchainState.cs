using System.Linq;

namespace BitcoinUtilities.Storage
{
    /// <summary>
    /// Immutable class that holds information about blockchain state.
    /// </summary>
    public class BlockchainState
    {
        //todo: make this class internal and move related generic storage tests to TestUtils or make Blockchain.State public

        public BlockchainState(StoredBlock bestHeader, StoredBlock bestChain)
        {
            BestHeader = bestHeader;
            BestChain = bestChain;
        }

        //todo: properties names are confusing

        public StoredBlock BestHeader { get; }

        public StoredBlock BestChain { get; }

        public BlockchainState SetBestHeader(StoredBlock newBestHeader)
        {
            return new BlockchainState(newBestHeader, BestChain);
        }

        public BlockchainState SetBestChain(StoredBlock newBestChain)
        {
            return new BlockchainState(BestHeader, newBestChain);
        }

        /// <summary>
        /// Returns an updated state, with replaced blocks.
        /// </summary>
        /// <param name="oldBlock">The block to replace.</param>
        /// <param name="newBlock">The replacing block.</param>
        /// <returns> If replacement affects state, then a new state instance is returned; otherwise, current instance is returned.</returns>
        public BlockchainState Update(StoredBlock oldBlock, StoredBlock newBlock)
        {
            if (Contains(oldBlock))
            {
                return new BlockchainState(
                    Matches(BestHeader, oldBlock) ? newBlock : BestHeader,
                    Matches(BestChain, oldBlock) ? newBlock : BestChain);
            }
            return this;
        }

        private bool Contains(StoredBlock block)
        {
            return Matches(BestHeader, block) || Matches(BestChain, block);
        }

        private bool Matches(StoredBlock block1, StoredBlock block2)
        {
            if (block1 == null || block2 == null)
            {
                return false;
            }
            return block1.Hash.SequenceEqual(block2.Hash);
        }
    }
}