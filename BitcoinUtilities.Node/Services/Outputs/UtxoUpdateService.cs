using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node.Events;
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
        public IReadOnlyCollection<IEventHandlingService> CreateForNode(BitcoinNode node)
        {
            return new IEventHandlingService[] {new UtxoUpdateService(node.EventServiceController, node.Blockchain, node.UtxoStorage)};
        }

        public IReadOnlyCollection<IEventHandlingService> CreateForEndpoint(BitcoinNode node, BitcoinEndpoint endpoint)
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

        // todo: add specific type for node-scoped services?
        public UtxoUpdateService(EventServiceController controller, Blockchain2 blockchain, UtxoStorage utxoStorage)
        {
            this.controller = controller;
            this.blockchain = blockchain;
            this.utxoStorage = utxoStorage;

            // todo: OnStart(RequestBlocks);
            // todo: On<BestHeaderChanged>(check best head && RequestBlocks);
            On<BlockAvailableEvent>(AddRequestedBlock);
        }

        public override void OnStart()
        {
            base.OnStart();
            RequestBlocks();
        }

        private void RequestBlocks()
        {
            HeaderSubChain chain = GetChainForUpdate();
            if (chain == null)
            {
                return;
            }

            controller.Raise(new PrefetchBlocksEvent(this, chain));
            controller.Raise(new RequestBlockEvent(this, chain.First().Hash));
        }

        private HeaderSubChain GetChainForUpdate()
        {
            UtxoHeader utxoHead = utxoStorage.GetLastHeader();

            HeaderSubChain chain = null;

            // best head can become invalid in the process, but there always be another best head
            while (chain == null)
            {
                DbHeader blockChainHead = blockchain.GetBestHead();

                if (utxoHead == null)
                {
                    // no UTXO yet, getting best chain from root
                    // todo: skip last blocks (chain can be long for new node)
                    chain = blockchain.GetSubChain(blockChainHead.Hash, blockChainHead.Height + 1);
                }
                else
                {
                    if (blockChainHead.Height <= utxoHead.Height)
                    {
                        // todo: allow reversal
                        return null;
                    }

                    // todo: skip last blocks (blockChainHead.Height - utxoHead.Height can be big for new node)
                    chain = blockchain.GetSubChain(blockChainHead.Hash, blockChainHead.Height - utxoHead.Height);

                    if (chain != null)
                    {
                        if (!ByteArrayComparer.Instance.Equals(chain.First().ParentHash, utxoHead.Hash))
                        {
                            // UTXO head is on the the wrong branch
                            // todo: allow reversal
                            return null;
                        }
                    }
                }
            }

            // todo: this should not be required after fixes above
            return new HeaderSubChain(chain.Take(2000));
        }

        private void AddRequestedBlock(BlockAvailableEvent evt)
        {
            HeaderSubChain chain = GetChainForUpdate();

            if (chain != null)
            {
                DbHeader firstHeader = chain.First();
                if (ByteArrayComparer.Instance.Equals(firstHeader.Hash, evt.HeaderHash))
                {
                    utxoStorage.Update(CreateAndValidateUtxoUpdate(firstHeader, evt.Block));
                    controller.Raise(new UtxoChangedEvent(firstHeader));
                }
            }

            RequestBlocks();
        }

        private UtxoUpdate CreateAndValidateUtxoUpdate(DbHeader header, BlockMessage block)
        {
            List<byte[]> outPoints = new List<byte[]>();
            foreach (Tx tx in block.Transactions)
            {
                foreach (var input in tx.Inputs)
                {
                    outPoints.Add(UtxoOutput.CreateOutPoint(input.PreviousOutput));
                }
            }

            Dictionary<byte[], UtxoOutput> existingOutputs = utxoStorage.GetUnspentOutputs(outPoints).ToDictionary(
                o => o.OutputPoint, ByteArrayComparer.Instance
            );

            Dictionary<byte[], UtxoOutput> newOutputs = new Dictionary<byte[], UtxoOutput>(ByteArrayComparer.Instance);

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
                        byte[] outPoint = UtxoOutput.CreateOutPoint(input.PreviousOutput);

                        if (!existingOutputs.TryGetValue(outPoint, out var oldOutput))
                        {
                            logger.Debug($"Transaction '{HexUtils.GetString(txHash)}' attempts to spend a non-existing output '{HexUtils.GetString(outPoint)}'.");
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