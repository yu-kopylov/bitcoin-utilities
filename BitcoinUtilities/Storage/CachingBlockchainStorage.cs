using System.Collections.Generic;
using System.Linq;

namespace BitcoinUtilities.Storage
{
    public class CachingBlockchainStorage : IBlockchainStorage
    {
        private readonly IBlockchainStorage storage;

        private Subchain lastChain;

        // this value is related to Blockchain.AnalyzedSubchainLength
        private int CachedChainMinLength = 2048;
        private int CachedChainMaxLength = 4096;

        /// <summary>
        /// Clears cached values.
        /// </summary>
        public void Reset()
        {
            lastChain = null;
        }

        public CachingBlockchainStorage(IBlockchainStorage storage)
        {
            this.storage = storage;
        }

        public BlockLocator GetCurrentChainLocator()
        {
            return storage.GetCurrentChainLocator();
        }

        public StoredBlock FindBlockByHash(byte[] hash)
        {
            return storage.FindBlockByHash(hash);
        }

        public StoredBlock FindBestHeaderChain()
        {
            return storage.FindBestHeaderChain();
        }

        public Subchain FindSubchain(byte[] hash, int length)
        {
            if (lastChain == null
                || (lastChain.Length < length && !lastChain.GetBlockByOffset(lastChain.Length - 1).Header.IsFirst)
                || !lastChain.GetBlockByOffset(0).Hash.SequenceEqual(hash))
            {
                lastChain = storage.FindSubchain(hash, length);
            }

            return lastChain.Clone();
        }

        public StoredBlock FindBlockByHeight(int height)
        {
            return storage.FindBlockByHeight(height);
        }

        public void AddBlock(StoredBlock block)
        {
            storage.AddBlock(block);

            //todo: add tests
            if (lastChain != null)
            {
                lastChain.Append(block);
                lastChain.Truncate(CachedChainMinLength, CachedChainMaxLength);
            }
        }

        public void UpdateBlock(StoredBlock block)
        {
            //todo: make StoredBlock immutable to avoid accidental changes to cached values?
            storage.UpdateBlock(block);
            if (lastChain != null)
            {
                //todo: it is a little bit ugly cashing
                List<StoredBlock> chainBlocks = new List<StoredBlock>(lastChain.Length);
                for (int i = lastChain.Length - 1; i >= 0; i--)
                {
                    StoredBlock chainBlock = lastChain.GetBlockByOffset(i);
                    if (chainBlock.Hash.SequenceEqual(block.Hash))
                    {
                        chainBlocks.Add(block);
                    }
                    else
                    {
                        chainBlocks.Add(chainBlock);
                    }
                }
                lastChain = new Subchain(chainBlocks);
            }
        }
    }
}