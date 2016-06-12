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

        private readonly TransactionalResource transactionalResource;

        public CachingBlockchainStorage(IBlockchainStorage storage)
        {
            this.storage = storage;
            transactionalResource = new TransactionalResource(OnCommit, OnRollback);
        }

        private void OnCommit()
        {
        }

        private void OnRollback()
        {
            Reset();
        }

        /// <summary>
        /// Clears cached values.
        /// </summary>
        private void Reset()
        {
            lastChain = null;
        }

        public BlockLocator GetCurrentChainLocator()
        {
            return storage.GetCurrentChainLocator();
        }

        public StoredBlock FindBlockByHash(byte[] hash)
        {
            return storage.FindBlockByHash(hash);
        }

        public StoredBlock FindFirst(BlockSelector selector)
        {
            return storage.FindFirst(selector);
        }

        public StoredBlock FindBestHeaderChain()
        {
            return storage.FindBestHeaderChain();
        }

        public List<StoredBlock> GetOldestBlocksWithoutContent(int maxCount)
        {
            return storage.GetOldestBlocksWithoutContent(maxCount);
        }

        public List<StoredBlock> GetBlocksByHeight(int[] heights)
        {
            return storage.GetBlocksByHeight(heights);
        }

        public Subchain FindSubchain(byte[] hash, int length)
        {
            // enlistment in transaction is not necessary, since cached data would remain valid until modifying method is called

            if (lastChain == null
                || (lastChain.Length < length && !lastChain.GetBlockByOffset(lastChain.Length - 1).Header.IsFirst)
                || !lastChain.GetBlockByOffset(0).Hash.SequenceEqual(hash))
            {
                lastChain = storage.FindSubchain(hash, length);
            }

            return lastChain?.Clone();
        }

        public StoredBlock FindBlockByHeight(int height)
        {
            return storage.FindBlockByHeight(height);
        }

        public void AddBlock(StoredBlock block)
        {
            transactionalResource.Enlist();

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
            transactionalResource.Enlist();

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

        public void AddBlockContent(byte[] hash, byte[] content)
        {
            transactionalResource.Enlist();

            //todo: make StoredBlock immutable to avoid accidental changes to cached values?
            storage.AddBlockContent(hash, content);
            if (lastChain != null)
            {
                //todo: it is a little bit ugly cashing
                for (int i = lastChain.Length - 1; i >= 0; i--)
                {
                    StoredBlock chainBlock = lastChain.GetBlockByOffset(i);
                    chainBlock.HasContent = true;
                }
            }
        }
    }
}