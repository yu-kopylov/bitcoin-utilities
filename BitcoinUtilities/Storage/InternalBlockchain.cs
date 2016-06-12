using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;

namespace BitcoinUtilities.Storage
{
    /// <summary>
    /// This class implements part of blockchain logic that requires access to storage.
    /// <para/>
    /// This class is not thread-safe except for <see cref="CommitedState"/> property.
    /// </summary>
    public class InternalBlockchain
    {
        //todo: make this class internal and move related generic storage tests to TestUtils

        private readonly IBlockchainStorage storage;

        private readonly TransactionalResource transactionalResource;

        private readonly object stateLock = new object();

        private BlockchainState commitedState;

        private BlockchainState currentState;

        public InternalBlockchain(IBlockchainStorage storage)
        {
            this.storage = storage;
            transactionalResource = new TransactionalResource(OnCommit, OnRollback);
        }

        public BlockchainState CommitedState
        {
            get
            {
                lock (stateLock)
                {
                    return commitedState;
                }
            }
        }

        private void OnCommit()
        {
            lock (stateLock)
            {
                commitedState = currentState;
            }
        }

        private void OnRollback()
        {
            lock (stateLock)
            {
                currentState = commitedState;
            }
        }

        public void Init()
        {
            transactionalResource.Enlist();

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
            var bestHeader = storage.FindFirst(BlockSelector.LastBestHeader);
            currentState = new BlockchainState(bestHeader);
        }

        public BlockLocator GetBlockLocator()
        {
            return storage.GetCurrentChainLocator();
        }

        public StoredBlock FindFirst(BlockSelector selector)
        {
            return storage.FindFirst(selector);
        }

        public List<StoredBlock> GetOldestBlocksWithoutContent(int maxCount)
        {
            return storage.GetOldestBlocksWithoutContent(maxCount);
        }

        public List<StoredBlock> GetBlocksByHeight(int[] heights)
        {
            return storage.GetBlocksByHeight(heights);
        }

        public List<StoredBlock> AddHeaders(List<StoredBlock> blocks)
        {
            transactionalResource.Enlist();

            List<StoredBlock> res = new List<StoredBlock>(blocks.Count);
            foreach (StoredBlock block in blocks)
            {
                res.Add(AddHeader(block));
            }
            return res;
        }

        public void AddBlockContent(BlockMessage block)
        {
            transactionalResource.Enlist();

            byte[] blockHash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(block.BlockHeader.Write));

            StoredBlock storedBlock = storage.FindBlockByHash(blockHash);
            if (storedBlock == null || storedBlock.HasContent)
            {
                return;
            }

            if (!BlockContentValidator.IsMerkleTreeValid(block))
            {
                // If merkle tree is invalid than we cannot rely on its header hash to mark block in storage as invalid.
                throw new BitcoinProtocolViolationException($"Merkle tree did not pass validation (block: {BitConverter.ToString(blockHash)}).");
            }

            // since block is already stored in storage we assume that its header is valid

            if (!BlockContentValidator.IsValid(storedBlock, block))
            {
                //todo: mark chain as broken
                throw new BitcoinProtocolViolationException($"Block content was invalid (block: {BitConverter.ToString(blockHash)}).");
            }

            storage.AddBlockContent(storedBlock.Hash, BitcoinStreamWriter.GetBytes(block.Write));
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

            //todo: review transaction usage in this class
            storage.AddBlock(genesisBlock);
            //todo: use network specific genesis block
            storage.AddBlockContent(genesisBlock.Hash, GenesisBlock.Raw);
        }

        private StoredBlock AddHeader(StoredBlock block)
        {
            StoredBlock storedBlock = storage.FindBlockByHash(block.Hash);
            if (storedBlock != null)
            {
                return storedBlock;
            }

            Subchain parentChain = storage.FindSubchain(block.Header.PrevBlock, Blockchain.AnalyzedSubchainLength);
            if (parentChain == null)
            {
                return block;
            }

            StoredBlock parentBlock = parentChain.GetBlockByOffset(0);

            double blockWork = DifficultyUtils.DifficultyTargetToWork(DifficultyUtils.NBitsToTarget(block.Header.NBits));

            //todo: move this logic to StoredBlock (also add broken chain propagation)?
            block.Height = parentBlock.Height + 1;
            block.TotalWork = parentBlock.TotalWork + blockWork;

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
            if (!IsBetterHeaderThan(block, currentState.BestHeader))
            {
                return;
            }

            StoredBlock newHead = block;
            StoredBlock oldHead = currentState.BestHeader;

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

            currentState = currentState.SetBestHeader(block);
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