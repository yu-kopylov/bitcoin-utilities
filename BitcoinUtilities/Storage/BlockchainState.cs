namespace BitcoinUtilities.Storage
{
    /// <summary>
    /// Immutable class that holds information about blockchain state.
    /// </summary>
    public class BlockchainState
    {
        //todo: make this class internal and move related generic storage tests to TestUtils or make Blockchain.State public

        public BlockchainState(StoredBlock bestHeader)
        {
            BestHeader = bestHeader;
        }

        public StoredBlock BestHeader { get; }

        public BlockchainState SetBestHeader(StoredBlock newBestHeader)
        {
            return new BlockchainState(newBestHeader);
        }
    }
}