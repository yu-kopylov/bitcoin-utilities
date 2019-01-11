using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BitcoinUtilities.Collections;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Node.Services.Headers;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Services.Blocks
{
    public class BlockDownloadServiceFactory : INodeEventServiceFactory
    {
        public IReadOnlyCollection<IEventHandlingService> CreateForNode(BitcoinNode node, CancellationToken cancellationToken)
        {
            return new IEventHandlingService[0];
        }

        public IReadOnlyCollection<IEventHandlingService> CreateForEndpoint(BitcoinNode node, BitcoinEndpoint endpoint)
        {
            return new IEventHandlingService[]
            {
                new BlockDownloadService(node.EventServiceController, node.Resources.Get<BlockRequestCollection>(), node.BlockStorage, endpoint)
            };
        }
    }

    public class BlockDownloadService : NodeEventHandlingService
    {
        private readonly EventServiceController controller;
        private readonly BlockRequestCollection requestCollection;
        private readonly BlockStorage storage;
        private readonly BitcoinEndpoint endpoint;

        private readonly LinkedDictionary<byte[], DateTime> inventory = new LinkedDictionary<byte[], DateTime>(ByteArrayComparer.Instance);
        private readonly LinkedDictionary<byte[], DateTime> sentRequests = new LinkedDictionary<byte[], DateTime>(ByteArrayComparer.Instance);

        public BlockDownloadService(
            EventServiceController controller,
            BlockRequestCollection requestCollection,
            BlockStorage storage,
            BitcoinEndpoint endpoint
        ) : base(endpoint)
        {
            this.controller = controller;
            this.requestCollection = requestCollection;
            this.storage = storage;
            this.endpoint = endpoint;

            On<BlockDownloadRequestedEvent>(e => CheckState());
            OnMessage<InvMessage>(ProcessInvMessage);
            OnMessage<BlockMessage>(ProcessBlockMessage);
        }

        public override void OnStart()
        {
            base.OnStart();
            CheckState();
        }

        private void CheckState()
        {
            // todo: is this method expensive of called too often?
            RemoveOutdatedEntries(inventory);
            RemoveOutdatedEntries(sentRequests);

            var pendingRequests = requestCollection.GetPendingRequests();

            int awaitableBlocksCount = pendingRequests.Count(h => sentRequests.ContainsKey(h.Hash));
            // todo: check constant
            if (awaitableBlocksCount >= 10)
            {
                return;
            }

            // todo: check constant
            List<DbHeader> tmp = pendingRequests.Where(h => inventory.ContainsKey(h.Hash) /*&& !sentRequests.ContainsKey(h.Hash)*/).ToList();
            List<DbHeader> requestableBlocks = tmp.Take(20) /*.Union(tmp.Skip(random.Next(10)).Take(49))*/.ToList();
            //var requestableBlocks = tmp.Skip(random.Next(50)).Take(20 + random.Next(30)).ToList();
            if (requestableBlocks.Any())
            {
                DateTime utcNow = DateTime.UtcNow;

                InventoryVector[] inventoryVectors = requestableBlocks.Select(b => new InventoryVector(InventoryVectorType.MsgBlock, b.Hash)).ToArray();
                endpoint.WriteMessage(new GetDataMessage(inventoryVectors));

                sentRequests.Clear();
                //inventory.Clear();
                foreach (var header in requestableBlocks)
                {
                    sentRequests.Add(header.Hash, utcNow);
                }

                //awaitableBlocksCount += requestableBlocks.Count;
                awaitableBlocksCount = requestableBlocks.Count;
                //return;
            }

            // todo: check constant
            if (awaitableBlocksCount < 10)
                //if (awaitableBlocksCount == 0)
            {
                byte[] locator = pendingRequests.FirstOrDefault()?.ParentHash;
                if (locator != null)
                {
                    endpoint.WriteMessage(new GetBlocksMessage(endpoint.ProtocolVersion, new byte[][] {locator}, new byte[32]));
                    inventory.Clear();
                }
            }
        }

        private void ProcessInvMessage(InvMessage message)
        {
            var advertisedBlocks = message.Inventory.Where(i => i.Type == InventoryVectorType.MsgBlock).Select(i => i.Hash);
            DateTime utcNow = DateTime.UtcNow;
            foreach (byte[] hash in advertisedBlocks)
            {
                inventory.Remove(hash);
                inventory.Add(hash, utcNow);
            }

            CheckState();
        }

        private void ProcessBlockMessage(BlockMessage blockMessage)
        {
            byte[] hash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(blockMessage.BlockHeader.Write));
            // todo: we would not need to serialize block if message included payload
            byte[] content = BitcoinStreamWriter.GetBytes(blockMessage.Write);

            // todo: validate block, disconnect endpoint if it is invalid, maybe mark header as invalid
            // todo: check for existance inside AddBlock
            storage.AddBlock(hash, content);
            // mark received should happen after saving block to storage
            if (requestCollection.MarkReceived(hash))
            {
                controller.Raise(new BlockAvailableEvent(hash, blockMessage));
            }

            inventory.Remove(hash);
            sentRequests.Remove(hash);

            CheckState();
        }

        private static void RemoveOutdatedEntries(LinkedDictionary<byte[], DateTime> entries)
        {
            //todo: check constants
            DateTime outdatedEntryDate = DateTime.UtcNow.AddSeconds(-10);
            List<byte[]> outdatedEntries = entries.Where(p => p.Value <= outdatedEntryDate).Select(p => p.Key).ToList();
            foreach (byte[] hash in outdatedEntries)
            {
                entries.Remove(hash);
            }

            // todo: handle dates from future

            //todo: check constants
            while (entries.Count > 5000)
            {
                entries.Remove(entries.First().Key);
            }
        }
    }
}