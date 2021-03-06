﻿using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Node.Modules.Headers;
using BitcoinUtilities.Node.Modules.Outputs.Events;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Threading;
using NLog;

namespace BitcoinUtilities.Node.Modules.Outputs
{
    public partial class UtxoUpdateService : EventHandlingService
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly IEventDispatcher eventDispatcher;
        private readonly Blockchain blockchain;
        private readonly UtxoRepository utxoRepository;

        private HeaderSubChain cachedChainForUpdate;

        private readonly PerformanceCounters performanceCounters = new PerformanceCounters(logger);

        private readonly Queue<UtxoUpdate> updatesAwaitingValidation = new Queue<UtxoUpdate>();

        private UtxoHeader lastProcessedHeader;
        private DbHeader lastRequestedHeader;

        public UtxoUpdateService(IEventDispatcher eventDispatcher, Blockchain blockchain, UtxoRepository utxoRepository)
        {
            this.eventDispatcher = eventDispatcher;
            this.blockchain = blockchain;
            this.utxoRepository = utxoRepository;

            // todo: do not call RequestBlocks in response to BestHeadChangedEvent too often
            On<BestHeadChangedEvent>(e => RequestBlocks());
            On<BlockAvailableEvent>(AddRequestedBlock);
            On<SignatureValidationResponse>(SaveValidatedBlock);
        }

        public override void OnStart()
        {
            base.OnStart();
            performanceCounters.StartRunning();

            lastProcessedHeader = utxoRepository.GetLastHeader();

            RequestBlocks();
        }

        private void RequestBlocks()
        {
            if (updatesAwaitingValidation.Count >= 16)
            {
                return;
            }

            HeaderSubChain chain = GetChainForUpdate(lastProcessedHeader);
            if (chain == null)
            {
                return;
            }

            int requiredBlockHeight = GetNextBlockHeight(lastProcessedHeader);
            DbHeader requiredHeader = chain.GetBlockByHeight(requiredBlockHeight);

            if (lastRequestedHeader == null || !lastRequestedHeader.Hash.SequenceEqual(requiredHeader.Hash))
            {
                lastRequestedHeader = requiredHeader;
                eventDispatcher.Raise(new PrefetchBlocksEvent(this, chain.GetChildSubChain(requiredBlockHeight, 2000)));
                eventDispatcher.Raise(new RequestBlockEvent(this, requiredHeader.Hash));
                performanceCounters.BlockRequestSent();
            }
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
                int requiredBlockHeight = GetNextBlockHeight(utxoHead);

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
            int requiredBlockHeight = GetNextBlockHeight(utxoHead);

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

            if (ProcessBlock(evt))
            {
                performanceCounters.BlockProcessed(evt.Block.Transactions.Length);
            }
            else
            {
                performanceCounters.BlockDiscarded();
            }

            RequestBlocks();
        }

        private bool ProcessBlock(BlockAvailableEvent evt)
        {
            var nextHeader = GetNextHeaderInChain(lastProcessedHeader);
            if (nextHeader == null || !nextHeader.Hash.SequenceEqual(evt.HeaderHash))
            {
                return false;
            }

            PrepareUpdate(nextHeader, evt.Block);
            return true;
        }

        private void PrepareUpdate(DbHeader header, BlockMessage block)
        {
            HashSet<byte[]> relatedTxHashes = new HashSet<byte[]>(ByteArrayComparer.Instance);

            foreach (Tx tx in block.Transactions)
            {
                foreach (var input in tx.Inputs)
                {
                    relatedTxHashes.Add(input.PreviousOutput.Hash);
                }

                relatedTxHashes.Add(tx.Hash);
            }

            IReadOnlyCollection<UtxoOutput> existingOutputs = utxoRepository.GetUnspentOutputs(relatedTxHashes);

            UpdatableOutputSet outputSet = new UpdatableOutputSet();
            outputSet.AppendExistingUnspentOutputs(existingOutputs);

            foreach (var unsavedUtxoUpdate in updatesAwaitingValidation)
            {
                Replay(outputSet, relatedTxHashes, unsavedUtxoUpdate);
            }

            TransactionProcessor txProcessor = new TransactionProcessor();
            var processedTransactions = txProcessor.UpdateOutputs(outputSet, header.Height, header.Hash, block);

            UtxoUpdate update = new UtxoUpdate(header.Height, header.Hash, header.ParentHash);
            update.Operations.AddRange(outputSet.Operations);

            updatesAwaitingValidation.Enqueue(update);

            lastProcessedHeader = new UtxoHeader(header.Hash, header.Height, true);

            eventDispatcher.Raise(new SignatureValidationRequest(header, processedTransactions));
        }

        private void Replay(UpdatableOutputSet outputSet, HashSet<byte[]> relatedTxHashes, UtxoUpdate unsavedUtxoUpdate)
        {
            foreach (UtxoOperation operation in unsavedUtxoUpdate.Operations)
            {
                UtxoOutput output = operation.Output;
                TxOutPoint outPoint = output.OutPoint;

                if (!relatedTxHashes.Contains(outPoint.Hash))
                {
                    continue;
                }

                if (operation.Spent)
                {
                    outputSet.Spend(output);
                }
                else
                {
                    outputSet.CreateUnspentOutput(outPoint.Hash, outPoint.Index, output.Value, output.PubkeyScript);
                }
            }

            outputSet.ClearOperations();
        }

        private void SaveValidatedBlock(SignatureValidationResponse validationResponse)
        {
            performanceCounters.SavingStarted();
            if (updatesAwaitingValidation.Count == 0 || !updatesAwaitingValidation.Peek().HeaderHash.SequenceEqual(validationResponse.Header.Hash))
            {
                throw new InvalidOperationException("Something went wrong, we got validation response not for the first block in queue.");
            }

            var update = updatesAwaitingValidation.Dequeue();

            if (!validationResponse.Valid)
            {
                // todo: mark block as invalid in blockchain and continue
                throw new InvalidOperationException($"Transactions of block '{HexUtils.GetReversedString(update.HeaderHash)} ({update.Height})' did not pass validation.");
            }

            utxoRepository.Update(update);

            RequestBlocks();
            performanceCounters.SavingComplete();
        }

        private DbHeader GetNextHeaderInChain(UtxoHeader currentHeader)
        {
            HeaderSubChain chain = GetChainForUpdate(currentHeader);

            if (chain == null)
            {
                return null;
            }

            int nextHeaderHeight = GetNextBlockHeight(currentHeader);
            DbHeader nextHeader = chain.GetBlockByHeight(nextHeaderHeight);
            return nextHeader;
        }

        private static int GetNextBlockHeight(UtxoHeader currentHeader)
        {
            return currentHeader == null ? 0 : currentHeader.Height + 1;
        }
    }
}