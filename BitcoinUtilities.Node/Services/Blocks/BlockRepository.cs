using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Services.Blocks
{
    public class BlockRepository : EventHandlingService
    {
        private readonly object monitor = new object();

        private readonly EventServiceController controller;
        private readonly BlockRequestCollection requestCollection;

        private readonly Dictionary<byte[], BlockMessage> unsavedBlocks = new Dictionary<byte[], BlockMessage>(ByteArrayComparer.Instance);

        public BlockRepository(EventServiceController controller, BlockRequestCollection requestCollection)
        {
            this.controller = controller;
            this.requestCollection = requestCollection;

            On<PrefetchBlocksEvent>(HandlePrefetchBlocksEvent);
            On<RequestBlockEvent>(HandleRequestBlockEvent);
        }

        private void HandlePrefetchBlocksEvent(PrefetchBlocksEvent evt)
        {
            lock (monitor)
            {
                var missingBlocks = evt.Headers.Where(h => !unsavedBlocks.ContainsKey(h.Hash)).ToList();
                // todo: also exclude headers that already exist in storage
                requestCollection.AddRequest(evt.RequestOwner, missingBlocks);
                controller.Raise(new BlockDownloadRequestedEvent());
            }
        }

        private void HandleRequestBlockEvent(RequestBlockEvent evt)
        {
            lock (monitor)
            {
                byte[] hash = evt.Hash;
                if (unsavedBlocks.TryGetValue(hash, out var block))
                {
                    controller.Raise(new BlockAvailableEvent(hash, block));
                }

                // todo: check storage

                requestCollection.SetRequired(hash);
            }
        }

        public void AddBlock(byte[] hash, BlockMessage block)
        {
            lock (monitor)
            {
                unsavedBlocks[hash] = block;
                if (unsavedBlocks.Count > 9999999)
                {
                    //todo: save to storage
                }

                if (requestCollection.MarkReceived(hash))
                {
                    controller.Raise(new BlockAvailableEvent(hash, block));
                }
            }
        }

        public BlockMessage GetBlock(byte[] hash)
        {
            lock (monitor)
            {
                if (unsavedBlocks.TryGetValue(hash, out var block))
                {
                    return block;
                }

                // todo: read from storage
                return null;
            }
        }
    }
}