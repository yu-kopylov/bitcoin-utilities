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
    }
}