using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BitcoinUtilities.Node.Events;
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
            return new IEventHandlingService[] {new BlockDownloadService(node.EventServiceController, endpoint)};
        }
    }

    public class BlockDownloadService : NodeEventHandlingService
    {
        private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);

        private readonly EventServiceController controller;
        private readonly BitcoinEndpoint endpoint;

        private DateTime? lastUnansweredRequest;

        public BlockDownloadService(EventServiceController controller, BitcoinEndpoint endpoint) : base(endpoint)
        {
            this.controller = controller;
            this.endpoint = endpoint;

            On<BlockDownloadRequestedEvent>(e => RequestBlocks(e.Locator));
            OnMessage<InvMessage>(ProcessInvMessage);
        }

        public override void OnStart()
        {
            base.OnStart();
            // todo: consider other pattern to access shared objects
            var blockStorageService = controller.GetService<BlockStorageService>();
            byte[] locator = blockStorageService?.LastDownloadLocator;
            if (locator != null)
            {
                RequestBlocks(locator);
            }
        }

        private void RequestBlocks(byte[] locator)
        {
            DateTime utcNow = DateTime.UtcNow;
            if (lastUnansweredRequest == null || (lastUnansweredRequest + RetryInterval) <= utcNow || lastUnansweredRequest > utcNow)
            {
                endpoint.WriteMessage(new GetBlocksMessage(endpoint.ProtocolVersion, new byte[][] {locator}, new byte[32]));
                lastUnansweredRequest = DateTime.UtcNow;
            }
        }

        private void ProcessInvMessage(InvMessage message)
        {
            var advertisedBlocks = message.Inventory.Where(i => i.Type == InventoryVectorType.MsgBlock).Select(i => i.Hash);
            if (advertisedBlocks.Any())
            {
                lastUnansweredRequest = null;
                InventoryVector[] inventoryVectors = advertisedBlocks.Select(b => new InventoryVector(InventoryVectorType.MsgBlock, b)).ToArray();
                endpoint.WriteMessage(new GetDataMessage(inventoryVectors));
            }
        }
    }
}