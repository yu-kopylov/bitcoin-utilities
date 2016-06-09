using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace BitcoinUtilities.Storage
{
    public class CachingBlockchainStorage : IBlockchainStorage
    {
        private readonly IBlockchainStorage storage;

        private Subchain lastChain;

        // this value is related to Blockchain.AnalyzedSubchainLength
        private int CachedChainMinLength = 2048;
        private int CachedChainMaxLength = 4096;

        private readonly TransactionListener transactionListener;

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
            transactionListener = new TransactionListener(this);
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
            transactionListener.Enlist();

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
            transactionListener.Enlist();

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
            transactionListener.Enlist();

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

        private class TransactionListener : IEnlistmentNotification
        {
            private readonly CachingBlockchainStorage cache;

            private Transaction currentTransaction;

            public TransactionListener(CachingBlockchainStorage cache)
            {
                this.cache = cache;
            }

            public void Enlist()
            {
                Transaction tx = Transaction.Current;
                if (tx == null)
                {
                    //todo: describe this exception in XMLDOC and add test
                    throw new InvalidOperationException("Methods that modify storage content should be called within a transaction.");
                }
                if (currentTransaction != null)
                {
                    if (currentTransaction != tx)
                    {
                        throw new InvalidOperationException($"{nameof(CachingBlockchainStorage)} is already enlisted in a transaction.");
                    }
                    return;
                }
                currentTransaction = tx;
                tx.EnlistVolatile(this, EnlistmentOptions.None);
            }

            public void Prepare(PreparingEnlistment enlistment)
            {
                enlistment.Prepared();
            }

            public void Commit(Enlistment enlistment)
            {
                currentTransaction = null;
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                cache.Reset();
                currentTransaction = null;
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                cache.Reset();
                currentTransaction = null;
                enlistment.Done();
            }
        }
    }
}