using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P;
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

        private readonly IBlockchainStorage storage;

        public Blockchain(IBlockchainStorage storage)
        {
            this.storage = storage;
        }

        /// <summary>
        /// Loads required information from storage.
        /// <para/>
        /// Initializes storage if necessary.
        /// <para/>
        /// Consecutive calls reloads information from storage.
        /// </summary>
        public void Init()
        {
            lock (lockObject)
            {
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

        private void AddGenesisBlock()
        {
            //todo: use network specific genesis block
            StoredBlock genesisBlock = new StoredBlock(GenesisBlock.GetHeader());

            double blockWork = DifficultyUtils.DifficultyTargetToWork(DifficultyUtils.NBitsToTarget(genesisBlock.Header.NBits));

            genesisBlock.Height = 0;
            genesisBlock.TotalWork = blockWork;

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

            return block;
        }
    }
}