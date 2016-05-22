using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Storage
{
    public class Blockchain
    {
        private readonly object lockObject = new object();

        private readonly IBlockChainStorage storage;

        public Blockchain(IBlockChainStorage storage)
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

            double blockWork = NumberUtils.DifficultyTargetToWork(NumberUtils.NBitsToTarget(genesisBlock.Header.NBits));

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

            StoredBlock parentBlock = storage.FindBlockByHash(block.Header.PrevBlock);
            if (parentBlock == null)
            {
                return block;
            }

            double blockWork = NumberUtils.DifficultyTargetToWork(NumberUtils.NBitsToTarget(block.Header.NBits));

            //todo: move this logic to StoredBlock ?
            block.Height = parentBlock.Height + 1;
            block.TotalWork = parentBlock.Height + blockWork;

            storage.AddBlock(block);

            return block;
        }
    }
}