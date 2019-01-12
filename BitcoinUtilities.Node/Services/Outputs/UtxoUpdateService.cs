using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Node.Services.Blocks;
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

    public class UtxoUpdateService : EventHandlingService
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly EventServiceController controller;
        private readonly Blockchain2 blockchain;
        private readonly UtxoStorage utxoStorage;

        private HeaderSubChain cachedChainForUpdate;

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

            RequestBlocks();
        }

        private static int GetRequiredBlockHeight(UtxoHeader utxoHead)
        {
            return utxoHead == null ? 0 : utxoHead.Height + 1;
        }

        private UtxoUpdate CreateAndValidateUtxoUpdate(DbHeader header, BlockMessage block)
        {
            List<TxOutPoint> outPoints = new List<TxOutPoint>();
            foreach (Tx tx in block.Transactions)
            {
                foreach (var input in tx.Inputs)
                {
                    outPoints.Add(input.PreviousOutput);
                }
            }

            Dictionary<TxOutPoint, UtxoOutput> existingOutputs = utxoStorage.GetUnspentOutputs(outPoints).ToDictionary(
                o => o.OutputPoint
            );

            Dictionary<TxOutPoint, UtxoOutput> newOutputs = new Dictionary<TxOutPoint, UtxoOutput>();

            UtxoUpdate update = new UtxoUpdate(header.Height, header.Hash, header.ParentHash);

            for (int txNum = 0; txNum < block.Transactions.Length; txNum++)
            {
                Tx tx = block.Transactions[txNum];
                byte[] txHash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(tx.Write));

                // todo: recheck specification for coinbase transactions
                ulong inputSum = 0;
                if (txNum != 0)
                {
                    foreach (TxIn input in tx.Inputs)
                    {
                        TxOutPoint outPoint = input.PreviousOutput;

                        if (!existingOutputs.TryGetValue(outPoint, out var oldOutput))
                        {
                            logger.Debug($"Transaction '{HexUtils.GetString(txHash)}' attempts to spend a non-existing output '{HexUtils.GetString(outPoint.Hash)}:{outPoint.Index}'.");
                            // todo: throw exception?
                            blockchain.MarkInvalid(header.Hash);
                            return null;
                        }

                        inputSum += oldOutput.Value;
                        existingOutputs.Remove(outPoint);
                        if (!newOutputs.Remove(outPoint))
                        {
                            update.SpentOutputs.Add(oldOutput);
                        }
                    }
                }

                ulong outputSum = 0;
                for (int outputIndex = 0; outputIndex < tx.Outputs.Length; outputIndex++)
                {
                    TxOut txOut = tx.Outputs[outputIndex];
                    outputSum += txOut.Value;

                    UtxoOutput newOutput = new UtxoOutput(txHash, outputIndex, header.Height, txOut);
                    update.NewOutputs.Add(newOutput);
                    existingOutputs.Add(newOutput.OutputPoint, newOutput);
                    newOutputs.Add(newOutput.OutputPoint, newOutput);
                }

                // todo: check output of coinbase transaction ?
                if (txNum != 0 && outputSum > inputSum)
                {
                    // todo: throw exception?
                    blockchain.MarkInvalid(header.Hash);
                    return null;
                }
            }

            return update;
        }
    }
}