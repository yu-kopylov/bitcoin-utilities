using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Scripts;
using NLog;

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

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly NetworkParameters networkParameters;

        private readonly IBlockchainStorage storage;

        private readonly TransactionalResource transactionalResource;

        private readonly object stateLock = new object();

        private BlockchainState commitedState;

        private BlockchainState currentState;

        public InternalBlockchain(NetworkParameters networkParameters, IBlockchainStorage storage)
        {
            this.networkParameters = networkParameters;
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
            var bestChain = storage.FindFirst(BlockSelector.LastChainBlock);
            currentState = new BlockchainState(bestHeader, bestChain);
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

            StoredBlock updatedBlock = storage.AddBlockContent(storedBlock.Hash, BitcoinStreamWriter.GetBytes(block.Write));
            //todo: test state updates
            currentState = currentState.Update(storedBlock, updatedBlock);
        }

        public bool Include(byte[] hash)
        {
            transactionalResource.Enlist();

            StoredBlock block = storage.FindBlockByHash(hash);
            if (block == null || !block.HasContent)
            {
                return false;
            }
            if (block.IsInBestBlockChain)
            {
                // todo: what to return in this scenario
                return true;
            }
            if (block.Height != currentState.BestChain.Height + 1 || !currentState.BestChain.Hash.SequenceEqual(block.Header.PrevBlock))
            {
                return false;
            }

            byte[] content = storage.GetBlockContent(block.Hash);

            BlockMessage blockMessage = BitcoinStreamReader.FromBytes(content, BlockMessage.Read);

            UnspentOutputsUpdate unspentOutputsUpdate;
            try
            {
                unspentOutputsUpdate = PrepareUnspentOutputsUpdate(block, blockMessage);
            }
            catch (BitcoinProtocolViolationException ex)
            {
                logger.Warn(ex, "The block with height {0} was not included to the blockchain because of validation errors.", block.Height);
                //todo: mark block as invalid and update best headers chain
                return false;
            }

            unspentOutputsUpdate.Persist();

            block = SetIsInBestBlockChain(block, true);
            storage.UpdateBlock(block);

            currentState = currentState.SetBestChain(block);

            return true;
        }

        private UnspentOutputsUpdate PrepareUnspentOutputsUpdate(StoredBlock block, BlockMessage blockMessage)
        {
            ScriptParser parser = new ScriptParser();

            ulong inputsSum = GetBlockReward(block);
            ulong outputsSum = 0;

            UnspentOutputsUpdate update = new UnspentOutputsUpdate(storage);

            for (int transactionNumber = 0; transactionNumber < blockMessage.Transactions.Length; transactionNumber++)
            {
                Tx transaction = blockMessage.Transactions[transactionNumber];

                ulong transactionInputsSum = 0;
                ulong transactionOutputsSum = 0;

                byte[] transactionHash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(transaction.Write));

                List<UnspentOutput> unspentOutputs = update.FindUnspentOutputs(transactionHash);
                if (unspentOutputs.Any())
                {
                    //todo: use network settings
                    if (block.Height == 91842 || block.Height == 91880)
                    {
                        // this blocks are exceptions from BIP-30
                        foreach (UnspentOutput unspentOutput in unspentOutputs)
                        {
                            update.Spend(unspentOutput.TransactionHash, unspentOutput.OutputNumber, block);
                        }
                    }
                    else
                    {
                        throw new BitcoinProtocolViolationException(
                            $"The transaction '{BitConverter.ToString(transactionHash)}'" +
                            $" in block '{BitConverter.ToString(block.Hash)}'" +
                            $" has same hash as an existing unspent transaction (see BIP-30).");
                    }
                }

                //todo: check transaction hash against genesis block transaction hash
                if (transactionNumber != 0)
                {
                    foreach (TxIn input in transaction.Inputs)
                    {
                        UnspentOutput output = update.FindUnspentOutput(input.PreviousOutput.Hash, input.PreviousOutput.Index);
                        if (output == null)
                        {
                            throw new BitcoinProtocolViolationException(
                                $"The input of the transaction '{BitConverter.ToString(transactionHash)}'" +
                                $" in block '{BitConverter.ToString(block.Hash)}'" +
                                $" has been already spent or did not exist.");
                        }
                        //todo: check for overflow
                        transactionInputsSum += output.Sum;

                        List<ScriptCommand> inputCommands;
                        if (!parser.TryParse(input.SignatureScript, out inputCommands))
                        {
                            throw new BitcoinProtocolViolationException(
                                $"The transaction '{BitConverter.ToString(transactionHash)}'" +
                                $" in block '{BitConverter.ToString(block.Hash)}'" +
                                $" has an invalid signature script.");
                        }
                        if (inputCommands.Any(c => !IsValidSignatureCommand(c.Code)))
                        {
                            throw new BitcoinProtocolViolationException(
                                $"The transaction '{BitConverter.ToString(transactionHash)}'" +
                                $" in block '{BitConverter.ToString(block.Hash)}'" +
                                $" has forbidden commands in the signature script.");
                        }

                        //todo: check signature for the output
                        update.Spend(output.TransactionHash, output.OutputNumber, block);
                    }
                }

                for (int outputNumber = 0; outputNumber < transaction.Outputs.Length; outputNumber++)
                {
                    TxOut output = transaction.Outputs[outputNumber];
                    //todo: check for overflow
                    transactionOutputsSum += output.Value;

                    List<ScriptCommand> commands;
                    if (!parser.TryParse(output.PubkeyScript, out commands))
                    {
                        //todo: how Bitcoin Core works in this scenario?
                        throw new BitcoinProtocolViolationException(
                            $"The output of transaction '{BitConverter.ToString(transactionHash)}'" +
                            $" in block '{BitConverter.ToString(block.Hash)}'" +
                            $" has an invalid pubkey script script.");
                    }

                    UnspentOutput unspentOutput = UnspentOutput.Create(block, transaction, outputNumber);
                    update.Add(unspentOutput);
                }

                if (transactionNumber != 0 && transactionInputsSum < transactionOutputsSum)
                {
                    // for coinbase transaction output sum is checked later as part of total block inputs, outputs and reward sums equation
                    throw new BitcoinProtocolViolationException(
                        $"The sum of the inputs in the transaction '{BitConverter.ToString(transactionHash)}'" +
                        $" in block '{BitConverter.ToString(block.Hash)}'" +
                        $" is less than the sum of the outputs.");
                }

                //todo: check for overflow
                inputsSum += transactionInputsSum;
                //todo: check for overflow
                outputsSum += transactionOutputsSum;
            }

            if (inputsSum != outputsSum)
            {
                throw new BitcoinProtocolViolationException(
                    $"The sum of the inputs and the reward" +
                    $" in the block '{BitConverter.ToString(block.Hash)}'" +
                    $" does not match the sum of the outputs.");
            }

            return update;
        }

        private bool IsValidSignatureCommand(byte code)
        {
            // note: OP_RESERVED (0x50) is considered to be a push-only command
            return code <= BitcoinScript.OP_16;
        }

        private ulong GetBlockReward(StoredBlock block)
        {
            //todo: use network settings
            ulong reward = 5000000000;
            int height = block.Height;
            //todo: use network settings
            while (height >= 210000)
            {
                height -= 210000;
                reward /= 2;
            }
            return reward;
        }

        public bool TruncateTo(byte[] hash)
        {
            transactionalResource.Enlist();

            //todo: implement
            return false;
        }

        public void Truncate()
        {
            transactionalResource.Enlist();

            throw new NotImplementedException();
        }

        private void AddGenesisBlock()
        {
            //todo: use network specific genesis block
            StoredBlockBuilder blockBuilder = new StoredBlockBuilder(GenesisBlock.GetHeader());

            double blockWork = DifficultyUtils.DifficultyTargetToWork(DifficultyUtils.NBitsToTarget(blockBuilder.Header.NBits));

            blockBuilder.Height = 0;
            blockBuilder.TotalWork = blockWork;
            blockBuilder.IsInBestBlockChain = true;
            blockBuilder.IsInBestHeaderChain = true;

            StoredBlock genesisBlock = blockBuilder.Build();

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

            Subchain parentChain = storage.FindSubchain(block.Header.PrevBlock, BlockHeaderValidator.RequiredSubchainLength);
            if (parentChain == null)
            {
                return block;
            }

            StoredBlock parentBlock = parentChain.GetBlockByOffset(0);

            block = block.AppendAfter(parentBlock);

            byte[] checkpointHash = networkParameters.GetCheckpointHash(block.Height);
            if (checkpointHash != null && !ByteArrayComparer.Instance.Equals(block.Hash, checkpointHash))
            {
                throw new BitcoinProtocolViolationException(string.Format(
                    "Header hash '{0}' does not match hash for checkpoint '{1}' at height {2}.",
                    // todo: reverse byte order ?
                    HexUtils.GetString(block.Hash),
                    HexUtils.GetString(checkpointHash),
                    block.Height
                ));
            }

            if (!BlockHeaderValidator.IsValid(networkParameters, block, parentChain))
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
                    newHead = SetIsInBestHeaderChain(newHead, true);
                    storage.UpdateBlock(newHead);
                    newHead = storage.FindBlockByHash(newHead.Header.PrevBlock);
                }

                if (markOldHead)
                {
                    oldHead = SetIsInBestHeaderChain(oldHead, false);
                    storage.UpdateBlock(oldHead);
                    oldHead = storage.FindBlockByHash(oldHead.Header.PrevBlock);
                }
            }

            currentState = currentState.SetBestHeader(block);
        }

        private StoredBlock SetIsInBestHeaderChain(StoredBlock block, bool val)
        {
            StoredBlockBuilder builder = new StoredBlockBuilder(block);
            builder.IsInBestHeaderChain = val;
            StoredBlock newBlock = builder.Build();

            currentState = currentState.Update(block, newBlock);

            return newBlock;
        }

        private StoredBlock SetIsInBestBlockChain(StoredBlock block, bool val)
        {
            StoredBlockBuilder builder = new StoredBlockBuilder(block);
            builder.IsInBestBlockChain = val;
            StoredBlock newBlock = builder.Build();

            currentState = currentState.Update(block, newBlock);

            return newBlock;
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