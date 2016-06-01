namespace BitcoinUtilities.Storage
{
    //todo: add methods and XMLDOC
    public interface IBlockchainStorage
    {
        BlockLocator GetCurrentChainLocator();

        StoredBlock FindBlockByHash(byte[] hash);

        /// <summary>
        /// Searches for last block from the chain with the maximum total work.
        /// <para/>
        /// Chain with the maximum total work may not match the current blockchain if blocks in that chain are not yet validated.
        /// </summary>
        StoredBlock FindBestHeaderChain();

        /// <summary>
        /// Reads a subchain from the storage that ends with the block with the given header hash and has the given length.
        /// </summary>
        /// <param name="hash">The expected hash of the last block in chain.</param>
        /// <param name="length">The expected length of the subchain.</param>
        /// <returns>
        /// If storage does not have a block with the given hash then null.
        /// <para/>
        /// If storage has a block with the given header hash then a subchain is returned.
        /// <para/>
        /// The length of returned subchain can be lower then expected only if it starts with the genesis block.
        /// </returns>
        //todo: move this method to higher level class ? consider transaction boundaries
        Subchain FindSubchain(byte[] hash, int length);

        //todo: this method requires a definition of current chain (otherwise signature is wrong)
        StoredBlock FindBlockByHeight(int height);

        void AddBlock(StoredBlock block);

        //todo: describe exceptions in XMLDOC
        void UpdateBlock(StoredBlock block);
    }
}