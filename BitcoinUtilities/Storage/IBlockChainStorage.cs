using System;
using System.Collections.Generic;

namespace BitcoinUtilities.Storage
{
    //todo: add methods and XMLDOC
    public interface IBlockchainStorage
    {
        BlockLocator GetCurrentChainLocator();

        StoredBlock FindBlockByHash(byte[] hash);

        /// <summary>
        /// Finds first block that matches a given selector.
        /// </summary>
        /// <param name="selector">The selector.</param>
        /// <returns>The first block that matches selector; or null if there is no such block.</returns>
        /// <exception cref="ArgumentException">If the sort order is not defined.</exception>
        StoredBlock FindFirst(BlockSelector selector);

        /// <summary>
        /// Finds first blocks that match a given selector.
        /// </summary>
        /// <param name="selector">The selector.</param>
        /// <param name="count">The number of blocks to return. Should be greater than 0 and less then or equal to 256.</param>
        /// <returns>A list of blocks that match selector.</returns>
        /// <exception cref="ArgumentException">If the sort order is not defined, or if count is not valid.</exception>
        List<StoredBlock> Find(BlockSelector selector, int count);

        /// <summary>
        /// Searches for the oldest blocks on the best header chain that does not have content.
        /// </summary>
        /// <param name="maxCount">The maximum number of blocks to return.</param>
        List<StoredBlock> GetOldestBlocksWithoutContent(int maxCount);

        /// <summary>
        /// Searches for blocks on the best header chain with given heights.
        /// </summary>
        /// <param name="heights">The array of heights.</param>
        List<StoredBlock> GetBlocksByHeight(int[] heights);

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

        void AddBlockContent(byte[] hash, byte[] content);
    }
}