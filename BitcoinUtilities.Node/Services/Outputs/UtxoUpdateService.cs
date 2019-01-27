using System.Collections.Generic;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.Node.Services.Headers;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Scripts;
using BitcoinUtilities.Threading;
using NLog;

namespace BitcoinUtilities.Node.Services.Outputs
{
    public partial class UtxoUpdateService : EventHandlingService
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly IEventDispatcher eventDispatcher;
        private readonly Blockchain blockchain;
        private readonly UtxoStorage utxoStorage;

        private HeaderSubChain cachedChainForUpdate;

        private readonly PerformanceCounters performanceCounters = new PerformanceCounters(logger);

        public UtxoUpdateService(IEventDispatcher eventDispatcher, Blockchain blockchain, UtxoStorage utxoStorage)
        {
            this.eventDispatcher = eventDispatcher;
            this.blockchain = blockchain;
            this.utxoStorage = utxoStorage;

            // todo: do not call RequestBlocks in response to BestHeadChangedEvent too often
            On<BestHeadChangedEvent>(e => RequestBlocks());
            On<BlockAvailableEvent>(AddRequestedBlock);
        }

        public override void OnStart()
        {
            base.OnStart();
            performanceCounters.StartRunning();
            RequestBlocks();
        }

        private void RequestBlocks()
        {
            UtxoHeader utxoHead = utxoStorage.GetLastHeader();
            HeaderSubChain chain = GetChainForUpdate(utxoHead);
            if (chain == null)
            {
                return;
            }

            int requiredBlockHeight = GetRequiredBlockHeight(utxoHead);

            performanceCounters.BlockRequestSent();
            eventDispatcher.Raise(new PrefetchBlocksEvent(this, chain.GetChildSubChain(requiredBlockHeight, 2000)));
            eventDispatcher.Raise(new RequestBlockEvent(this, chain.GetBlockByHeight(requiredBlockHeight).Hash));
        }

        private HeaderSubChain GetChainForUpdate(UtxoHeader utxoHead)
        {
            if (CanUseCachedChainForUpdate(utxoHead))
            {
                return cachedChainForUpdate;
            }

            HeaderSubChain chain = null;

            // best head can become invalid in the process, but there always be another best head
            while (chain == null)
            {
                DbHeader blockChainHead = blockchain.GetBestHead();
                int requiredBlockHeight = GetRequiredBlockHeight(utxoHead);

                if (requiredBlockHeight > blockChainHead.Height)
                {
                    // todo: allow reversal
                    return null;
                }

                chain = blockchain.GetSubChain(blockChainHead.Hash, blockChainHead.Height - requiredBlockHeight + 1);

                if (chain != null && utxoHead != null)
                {
                    if (!ByteArrayComparer.Instance.Equals(chain.GetBlockByHeight(requiredBlockHeight).ParentHash, utxoHead.Hash))
                    {
                        // UTXO head is on the the wrong branch
                        // todo: allow reversal
                        return null;
                    }
                }
            }

            cachedChainForUpdate = chain;

            return chain;
        }

        private bool CanUseCachedChainForUpdate(UtxoHeader utxoHead)
        {
            if (cachedChainForUpdate == null)
            {
                return false;
            }

            DbHeader lastChainHead = cachedChainForUpdate.GetBlockByOffset(0);
            DbHeader blockChainHead = blockchain.GetBestHead();

            if (lastChainHead != blockChainHead)
            {
                return false;
            }

            var lastChainTail = cachedChainForUpdate.GetBlockByOffset(cachedChainForUpdate.Count - 1);
            int requiredBlockHeight = GetRequiredBlockHeight(utxoHead);

            if (requiredBlockHeight < lastChainTail.Height || requiredBlockHeight > lastChainHead.Height)
            {
                return false;
            }

            if (utxoHead == null)
            {
                // no need to check previous hash for the genesis block
                return true;
            }

            DbHeader requiredHeader = cachedChainForUpdate.GetBlockByHeight(requiredBlockHeight);
            return ByteArrayComparer.Instance.Equals(requiredHeader.ParentHash, utxoHead.Hash);
        }

        private void AddRequestedBlock(BlockAvailableEvent evt)
        {
            performanceCounters.BlockReceived();

            if (IncludeBlock(evt))
            {
                performanceCounters.BlockProcessed(evt.Block.Transactions.Length);
            }
            else
            {
                performanceCounters.BlockDiscarded();
            }

            RequestBlocks();
        }

        private bool IncludeBlock(BlockAvailableEvent evt)
        {
            UtxoHeader utxoHead = utxoStorage.GetLastHeader();
            HeaderSubChain chain = GetChainForUpdate(utxoHead);

            if (chain != null)
            {
                int requiredBlockHeight = GetRequiredBlockHeight(utxoHead);
                DbHeader requiredHeader = chain.GetBlockByHeight(requiredBlockHeight);
                if (ByteArrayComparer.Instance.Equals(requiredHeader.Hash, evt.HeaderHash))
                {
                    utxoStorage.Update(CreateAndValidateUtxoUpdate(requiredHeader, evt.Block));
                    eventDispatcher.Raise(new UtxoChangedEvent(requiredHeader));
                    return true;
                }
            }

            return false;
        }

        private static int GetRequiredBlockHeight(UtxoHeader utxoHead)
        {
            return utxoHead == null ? 0 : utxoHead.Height + 1;
        }

        private UtxoUpdate CreateAndValidateUtxoUpdate(DbHeader header, BlockMessage block)
        {
            HashSet<byte[]> relatedTxHashes = new HashSet<byte[]>(ByteArrayComparer.Instance);

            foreach (Tx tx in block.Transactions)
            {
                foreach (var input in tx.Inputs)
                {
                    relatedTxHashes.Add(input.PreviousOutput.Hash);
                }

                relatedTxHashes.Add(CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(tx.Write)));
            }

            IReadOnlyCollection<UtxoOutput> existingOutputs = utxoStorage.GetUnspentOutputs(relatedTxHashes);

            UpdatableOutputSet outputSet = new UpdatableOutputSet();
            outputSet.AppendExistingUnspentOutputs(existingOutputs);

            TransactionProcessor txProcessor = new TransactionProcessor();
            var processedTransactions = txProcessor.UpdateOutputs(outputSet, header.Height, header.Hash, block);

            VerifySignatures(header.Hash, processedTransactions);

            UtxoUpdate update = new UtxoUpdate(header.Height, header.Hash, header.ParentHash);
            update.ExistingSpentOutputs.AddRange(outputSet.ExistingSpentOutputs);
            update.CreatedUnspentOutputs.AddRange(outputSet.CreatedUnspentOutputs);

            return update;
        }

        private void VerifySignatures(byte[] blockHash, ProcessedTransaction[] processedTransactions)
        {
            var enumerator = new ConcurrentEnumerator<ProcessedTransaction>(processedTransactions);
            enumerator.ProcessWith(new object[4], (_, en) => VerifySignatures(blockHash, en));
        }

        private object VerifySignatures(byte[] blockHash, IConcurrentEnumerator<ProcessedTransaction> processedTransactions)
        {
            while (processedTransactions.GetNext(out var transaction))
            {
                // explicitly define coinbase transaction?
                if (transaction.Inputs.Length != 0)
                {
                    for (int inputIndex = 0; inputIndex < transaction.Inputs.Length; inputIndex++)
                    {
                        if (!VerifySignature(transaction, inputIndex))
                        {
                            throw new BitcoinProtocolViolationException(
                                $"The transaction '{HexUtils.GetString(transaction.TxHash)}'" +
                                $" in block '{HexUtils.GetString(blockHash)}'" +
                                $" has an invalid signature script for input {inputIndex}.");
                        }
                    }
                }
            }

            return null;
        }

        private bool VerifySignature(ProcessedTransaction transaction, int inputIndex)
        {
            ISigHashCalculator sigHashCalculator = new BitcoinCoreSigHashCalculator(transaction.Transaction);

            sigHashCalculator.InputIndex = inputIndex;
            sigHashCalculator.Amount = transaction.Inputs[inputIndex].Value;

            ScriptProcessor scriptProcessor = new ScriptProcessor();
            scriptProcessor.SigHashCalculator = sigHashCalculator;

            scriptProcessor.Execute(transaction.Transaction.Inputs[inputIndex].SignatureScript);
            scriptProcessor.Execute(transaction.Inputs[inputIndex].PubKeyScript);

            return scriptProcessor.Valid && scriptProcessor.Success;
        }
    }
}