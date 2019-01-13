using System.Collections.Generic;
using System.Threading;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.Node.Services.Headers;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Threading;
using NLog;

namespace BitcoinUtilities.Node.Services.Outputs
{
    public class UtxoUpdateServiceFactory : INodeEventServiceFactory
    {
        public void CreateResources(BitcoinNode node)
        {
        }

        public IReadOnlyCollection<IEventHandlingService> CreateNodeServices(BitcoinNode node, CancellationToken cancellationToken)
        {
            return new IEventHandlingService[] {new UtxoUpdateService(node.EventServiceController, node.Blockchain, node.UtxoStorage)};
        }

        public IReadOnlyCollection<IEventHandlingService> CreateEndpointServices(BitcoinNode node, BitcoinEndpoint endpoint)
        {
            return new IEventHandlingService[0];
        }
    }

    public partial class UtxoUpdateService : EventHandlingService
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly EventServiceController controller;
        private readonly Blockchain2 blockchain;
        private readonly UtxoStorage utxoStorage;

        private HeaderSubChain cachedChainForUpdate;

        private readonly PerformanceCounters performanceCounters = new PerformanceCounters(logger);

        public UtxoUpdateService(EventServiceController controller, Blockchain2 blockchain, UtxoStorage utxoStorage)
        {
            this.controller = controller;
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
            controller.Raise(new PrefetchBlocksEvent(this, chain.GetChildSubChain(requiredBlockHeight, 2000)));
            controller.Raise(new RequestBlockEvent(this, chain.GetBlockByHeight(requiredBlockHeight).Hash));
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

            UtxoHeader utxoHead = utxoStorage.GetLastHeader();
            HeaderSubChain chain = GetChainForUpdate(utxoHead);

            if (chain != null)
            {
                int requiredBlockHeight = GetRequiredBlockHeight(utxoHead);
                DbHeader requiredHeader = chain.GetBlockByHeight(requiredBlockHeight);
                if (ByteArrayComparer.Instance.Equals(requiredHeader.Hash, evt.HeaderHash))
                {
                    utxoStorage.Update(CreateAndValidateUtxoUpdate(requiredHeader, evt.Block));
                    controller.Raise(new UtxoChangedEvent(requiredHeader));
                }
            }

            performanceCounters.BlockProcessed(evt.Block.Transactions.Length);

            RequestBlocks();
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
            txProcessor.UpdateOutputs(outputSet, header.Height, header.Hash, block);

            UtxoUpdate update = new UtxoUpdate(header.Height, header.Hash, header.ParentHash);
            update.ExistingSpentOutputs.AddRange(outputSet.ExistingSpentOutputs);
            update.CreatedUnspentOutputs.AddRange(outputSet.CreatedUnspentOutputs);

            return update;
        }
    }
}