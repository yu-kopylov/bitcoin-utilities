using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Node.Services.Headers;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Services.Blocks
{
    public class BlockStorageServiceFactory : INodeEventServiceFactory
    {
        public IReadOnlyCollection<IEventHandlingService> CreateForNode(BitcoinNode node, CancellationToken cancellationToken)
        {
            return new IEventHandlingService[] {new BlockStorageService(node.EventServiceController, node.BlockStorage)};
        }

        public IReadOnlyCollection<IEventHandlingService> CreateForEndpoint(BitcoinNode node, BitcoinEndpoint endpoint)
        {
            return new IEventHandlingService[0];
        }
    }

    public class BlockStorageService : EventHandlingService
    {
        private readonly EventServiceController controller;
        private readonly BlockStorage storage;

        // todo: remember owner of request
        private readonly HashSet<byte[]> requestedBlocks = new HashSet<byte[]>(ByteArrayComparer.Instance);

        public BlockStorageService(EventServiceController controller, BlockStorage storage)
        {
            this.controller = controller;
            this.storage = storage;

            On<RequestBlockEvent>(SaveRequest);
            On<PrefetchBlocksEvent>(UpdateRequests);
            On<MessageEvent>(e => e.Message is BlockMessage, SaveBlock);
        }

        // todo: use different mechanism to share data
        private volatile byte[] lastDownloadLocator;

        public byte[] LastDownloadLocator
        {
            get { return lastDownloadLocator; }
        }

        private void UpdateRequests(PrefetchBlocksEvent evt)
        {
            // todo: replace with real implementation
            DbHeader firstHeader = evt.Headers.FirstOrDefault();
            if (firstHeader != null)
            {
                lastDownloadLocator = firstHeader.ParentHash;
                controller.Raise(new BlockDownloadRequestedEvent(firstHeader.ParentHash));
            }
        }

        private void SaveRequest(RequestBlockEvent evt)
        {
            byte[] block = storage.GetBlock(evt.Hash);
            if (block != null)
            {
                controller.Raise(new BlockAvailableEvent(evt.Hash, BitcoinStreamReader.FromBytes(block, BlockMessage.Read)));
            }
            else
            {
                requestedBlocks.Add(evt.Hash);
            }
        }

        private void SaveBlock(MessageEvent message)
        {
            BlockMessage blockMessage = (BlockMessage) message.Message;
            byte[] hash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(blockMessage.BlockHeader.Write));
            // todo: we would not need to serialize block if message included payload
            byte[] content = BitcoinStreamWriter.GetBytes(blockMessage.Write);

            // todo: validate block, disconnect endpoint if it is invalid, maybe mark header as invalid
            // todo: check for existance inside AddBlock
            if (storage.GetBlock(hash) == null)
            {
                storage.AddBlock(hash, content);
            }

            if (requestedBlocks.Remove(hash))
            {
                controller.Raise(new BlockAvailableEvent(hash, blockMessage));
            }
        }
    }
}