using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Storage
{
    /// <summary>
    /// All public methods are thread-safe.
    /// </summary>
    public class Blockchain
    {
        //todo: improve XMLDOC for this class

        /* Lower Boundaries:
         * 2016 - When difficulty changes, we need the 2016th block before the current one. Source: https://en.bitcoin.it/wiki/Protocol_rules#Difficulty_change .
         * 11 - A timestamp is accepted as valid if it is greater than the median timestamp of previous 11 blocks. Source: https://en.bitcoin.it/wiki/Block_timestamp .
         */
        private const int AnalyzedSubchainLength = 2016;

        private readonly object lockObject = new object();

        private readonly CachingBlockchainStorage storage;

        //todo: just volatile here without a lock to avoid delays on GUI, is there a better way?
        private volatile StoredBlock bestHeader;

        public Blockchain(IBlockchainStorage storage)
        {
            this.storage = new CachingBlockchainStorage(storage);
        }

        public StoredBlock BestHeader
        {
            get { return bestHeader; }
        }

        public event Action BestHeaderChanged;

        /// <summary>
        /// Resets internal caches and loads required information from a storage.
        /// <para/>
        /// Initializes the storage if necessary.
        /// <para/>
        /// Consecutive calls reloads information from the storage.
        /// </summary>
        public void Init()
        {
            lock (lockObject)
            {
                storage.Reset();

                StoredBlock genesisBlock = storage.FindBlockByHeight(0);

                if (genesisBlock == null)
                {
                    AddGenesisBlock();
                }
                else if (!GenesisBlock.Hash.SequenceEqual(genesisBlock.Hash))
                {
                    //todo: also check for situation when there is no genisis block in storage, but storage is not empty
                    throw new InvalidOperationException("The genesis block in storage has wrong hash.");
                }

                bestHeader = storage.FindBestHeaderChain();
                //todo: event call within a lock?
                BestHeaderChanged?.Invoke();
            }
        }

        /// <summary>
        /// Returns <see cref="BlockLocator"/> for current active chain.
        /// </summary>
        public BlockLocator GetBlockLocator()
        {
            //todo: specify what does active in XMLDOC means
            lock (lockObject)
            {
                return storage.GetCurrentChainLocator();
            }
        }

        /// <summary>
        /// Searches for the oldest blocks on the best header chain that does not have content.
        /// </summary>
        /// <param name="maxCount">The maximum number of blocks to return.</param>
        public List<StoredBlock> GetOldestBlocksWithoutContent(int maxCount)
        {
            lock (lockObject)
            {
                return storage.GetOldestBlocksWithoutContent(maxCount);
            }
        }

        /// <summary>
        /// Searches for blocks on the best header chain with given heights.
        /// </summary>
        /// <param name="heights">The array of heights.</param>
        public List<StoredBlock> GetBlocksByHeight(int[] heights)
        {
            lock (lockObject)
            {
                return storage.GetBlocksByHeight(heights);
            }
        }

        /// <summary>
        /// Attempts to add the given block headers to this blockchain.
        /// <para/>
        /// Blocks that cannot be appended to this blockchain are ignored.
        /// </summary>
        /// <param name="headers">The block headers to add.</param>
        /// <exception cref="BitcoinProtocolViolationException">If at least one header did not pass validation.</exception>
        /// <returns>Storage information about each given header.</returns>
        public List<StoredBlock> AddHeaders(IEnumerable<BlockHeader> headers)
        {
            //todo: review XMLDOC and implementation

            List<StoredBlock> blocks = new List<StoredBlock>();

            foreach (BlockHeader header in headers)
            {
                if (!BlockHeaderValidator.IsValid(header))
                {
                    throw new BitcoinProtocolViolationException("An invalid header was received.");
                }
                //todo: also check nBits against current network difficulty
                blocks.Add(new StoredBlock(header));
            }

            //todo: perform topological sorting of blocks?

            List<StoredBlock> res = new List<StoredBlock>();

            //todo: consider reducing contention by using reader-writer lock
            lock (lockObject)
            {
                foreach (StoredBlock block in blocks)
                {
                    res.Add(AddHeader(block));
                }
            }

            return res;
        }

        public void AddBlockContent(BlockMessage block)
        {
            if (BlockContentValidator.IsMerkleTreeValid(block))
            {
                // If merkle tree is invalid than we cannot rely on header hash to mark block in storage as invalid.
                throw new BitcoinProtocolViolationException("Merkle tree did not pass validation.");
            }
            //todo: validate
            //todo: save
        }

        private void AddGenesisBlock()
        {
            //todo: use network specific genesis block
            StoredBlock genesisBlock = new StoredBlock(GenesisBlock.GetHeader());

            double blockWork = DifficultyUtils.DifficultyTargetToWork(DifficultyUtils.NBitsToTarget(genesisBlock.Header.NBits));

            genesisBlock.Height = 0;
            genesisBlock.TotalWork = blockWork;
            genesisBlock.IsInBestBlockChain = true;
            genesisBlock.IsInBestHeaderChain = true;

            //todo: also add transactions for this block
            storage.AddBlock(genesisBlock);
        }

        private StoredBlock AddHeader(StoredBlock block)
        {
            StoredBlock storedBlock = storage.FindBlockByHash(block.Hash);
            if (storedBlock != null)
            {
                return storedBlock;
            }

            Subchain parentChain = storage.FindSubchain(block.Header.PrevBlock, AnalyzedSubchainLength);
            if (parentChain == null)
            {
                return block;
            }

            StoredBlock parentBlock = parentChain.GetBlockByOffset(0);

            double blockWork = DifficultyUtils.DifficultyTargetToWork(DifficultyUtils.NBitsToTarget(block.Header.NBits));

            //todo: move this logic to StoredBlock ?
            block.Height = parentBlock.Height + 1;
            block.TotalWork = parentBlock.Height + blockWork;

            if (!BlockHeaderValidator.IsValid(block, parentChain))
            {
                throw new BitcoinProtocolViolationException("An invalid header was received.");
            }

            storage.AddBlock(block);

            UpdateBestHeadersChain(block);

            return block;
        }

        private void UpdateBestHeadersChain(StoredBlock block)
        {
            if (!IsBetterHeaderThan(block, bestHeader))
            {
                return;
            }

            StoredBlock newHead = block;
            StoredBlock oldHead = bestHeader;

            //todo: this code should be wrapped in transaction, init should be called on failure
            while (!newHead.Hash.SequenceEqual(oldHead.Hash))
            {
                bool markNewHead = newHead.Height >= oldHead.Height;
                bool markOldHead = oldHead.Height >= newHead.Height;

                if (markNewHead)
                {
                    newHead.IsInBestHeaderChain = true;
                    storage.UpdateBlock(newHead);
                    newHead = storage.FindBlockByHash(newHead.Header.PrevBlock);
                }

                if (markOldHead)
                {
                    oldHead.IsInBestHeaderChain = false;
                    storage.UpdateBlock(oldHead);
                    oldHead = storage.FindBlockByHash(oldHead.Header.PrevBlock);
                }
            }

            bestHeader = block;

            //todo: event call within a lock?
            BestHeaderChanged?.Invoke();
        }

        private bool IsBetterHeaderThan(StoredBlock block1, StoredBlock block2)
        {
            //todo: are there any other conditions?
            if (block1.TotalWork > block2.TotalWork)
            {
                return true;
            }
            if (block1.TotalWork < block2.TotalWork)
            {
                return false;
            }
            return block1.Header.Timestamp < block2.Header.Timestamp;
        }
    }
}