using System.Collections.Generic;
using System.Threading;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Services.Blocks
{
    public class BlockStorageServiceFactory : INodeEventServiceFactory
    {
        public IReadOnlyCollection<IEventHandlingService> CreateForNode(BitcoinNode node, CancellationToken cancellationToken)
        {
            return new IEventHandlingService[]
            {
                new BlockStorageService(node.EventServiceController, node.Resources.Get<BlockRequestCollection>(), node.BlockStorage)
            };
        }

        public IReadOnlyCollection<IEventHandlingService> CreateForEndpoint(BitcoinNode node, BitcoinEndpoint endpoint)
        {
            return new IEventHandlingService[0];
        }
    }

    public class BlockStorageService : EventHandlingService
    {
        private readonly EventServiceController controller;
        private readonly BlockRequestCollection requestCollection;
        private readonly BlockStorage storage;

        public BlockStorageService(EventServiceController controller, BlockRequestCollection requestCollection, BlockStorage storage)
        {
            this.controller = controller;
            this.requestCollection = requestCollection;
            this.storage = storage;

            On<RequestBlockEvent>(SaveRequest);
            On<PrefetchBlocksEvent>(UpdateRequests);
        }

        private void UpdateRequests(PrefetchBlocksEvent evt)
        {
            // todo: exclude headers that already exist in storage
            requestCollection.AddRequest(evt.RequestOwner, evt.Headers);
            controller.Raise(new BlockDownloadRequestedEvent());
        }

        private void SaveRequest(RequestBlockEvent evt)
        {
            requestCollection.SetRequired(evt.Hash);
            byte[] block = storage.GetBlock(evt.Hash);
            if (block != null && requestCollection.MarkReceived(evt.Hash))
            {
                controller.Raise(new BlockAvailableEvent(evt.Hash, BitcoinStreamReader.FromBytes(block, BlockMessage.Read)));
            }
        }
    }
}