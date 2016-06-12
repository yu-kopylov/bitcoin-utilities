using System;
using System.Collections.Generic;
using System.Transactions;
using BitcoinUtilities.Collections;
using BitcoinUtilities.Node;
using BitcoinUtilities.Node.Rules;
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
        internal const int AnalyzedSubchainLength = 2016;

        private const int AddHeadersBatchSize = 50;

        private readonly object blockchainLock = new object();

        private readonly object stateLock = new object();

        private readonly InternalBlockchain blockchain;

        private volatile BlockchainState state;

        public Blockchain(IBlockchainStorage storage)
        {
            CachingBlockchainStorage cache = new CachingBlockchainStorage(storage);
            blockchain = new InternalBlockchain(cache);
        }

        public BlockchainState State
        {
            get
            {
                lock (stateLock)
                {
                    return state;
                }
            }
        }

        public event Action StateChanged;

        /// <summary>
        /// Loads state required from the storage.
        /// </summary>
        public void Init()
        {
            ExecuteInTransaction(blockchain.Init);
        }

        /// <summary>
        /// Finds first block that matches a given selector.
        /// </summary>
        /// <param name="selector">The selector.</param>
        /// <returns>The first block that matches selector; or null if there is no such block.</returns>
        /// <exception cref="ArgumentException">If the sort order is not defined.</exception>
        public StoredBlock FindFirst(BlockSelector selector)
        {
            //todo: use transaction?
            lock (blockchainLock)
            {
                return blockchain.FindFirst(selector);
            }
        }

        /// <summary>
        /// Returns <see cref="BlockLocator"/> for current active chain.
        /// </summary>
        public BlockLocator GetBlockLocator()
        {
            //todo: specify what does active in XMLDOC means
            lock (blockchainLock)
            {
                return blockchain.GetBlockLocator();
            }
        }

        /// <summary>
        /// Searches for the oldest blocks on the best header chain that does not have content.
        /// </summary>
        /// <param name="maxCount">The maximum number of blocks to return.</param>
        public List<StoredBlock> GetOldestBlocksWithoutContent(int maxCount)
        {
            lock (blockchainLock)
            {
                return blockchain.GetOldestBlocksWithoutContent(maxCount);
            }
        }

        /// <summary>
        /// Searches for blocks on the best header chain with given heights.
        /// </summary>
        /// <param name="heights">The array of heights.</param>
        public List<StoredBlock> GetBlocksByHeight(int[] heights)
        {
            lock (blockchainLock)
            {
                return blockchain.GetBlocksByHeight(heights);
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

            List<StoredBlock> res = new List<StoredBlock>(blocks.Count);

            foreach (List<StoredBlock> batch in blocks.Split(AddHeadersBatchSize))
            {
                ExecuteInTransaction(() => res.AddRange(blockchain.AddHeaders(batch)));
            }

            return res;
        }

        public void AddBlockContent(BlockMessage block)
        {
            //todo: add XMLDOC
            ExecuteInTransaction(() => blockchain.AddBlockContent(block));
        }

        /// <summary>
        /// Adds the block with the given hash to main chain.
        /// </summary>
        public void Include(byte[] hash)
        {
            ExecuteInTransaction(() => blockchain.Include(hash));
        }

        /// <summary>
        /// Performs fast truncation of the main chain to the given block using available rollback data.
        /// </summary>
        /// <param name="hash">The hash of the last block in the remaining chain.</param>
        /// <returns>true if chain was truncated; false if there is no data available for a fast rollback to the given block.</returns>
        /// <exception cref="InvalidOperationException">If the block with the given hash does not exist in main chain.</exception>
        public bool TruncateTo(byte[] hash)
        {
            bool res = false;
            ExecuteInTransaction(() => { res = blockchain.TruncateTo(hash); });
            return res;
        }

        /// <summary>
        /// Truncates the main chain to the genesis block.
        /// </summary>
        public void Truncate()
        {
            ExecuteInTransaction(blockchain.Truncate);
        }

        private void ExecuteInTransaction(Action action)
        {
            lock (blockchainLock)
            {
                using (var tx = new TransactionScope(TransactionScopeOption.Required))
                {
                    action();
                    tx.Complete();
                }
            }
            CheckState();
        }

        private void CheckState()
        {
            BlockchainState newState = blockchain.CommitedState;

            bool stateChanged = false;
            lock (stateLock)
            {
                if (newState != state)
                {
                    state = newState;
                    stateChanged = true;
                }
            }

            if (stateChanged)
            {
                StateChanged?.Invoke();
            }
        }
    }
}