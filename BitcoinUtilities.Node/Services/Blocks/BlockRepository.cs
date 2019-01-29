using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Collections;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Node.Services.Headers;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Services.Blocks
{
    public class BlockRepository : EventHandlingService
    {
        private const int MaxCachedBlocks = 3000;

        private readonly object monitor = new object();

        private readonly IEventDispatcher eventDispatcher;
        private readonly BlockRequestCollection requestCollection;

        private readonly LinkedDictionary<byte[], BlockMessage> blocks = new LinkedDictionary<byte[], BlockMessage>(ByteArrayComparer.Instance);

        public BlockRepository(IEventDispatcher eventDispatcher, BlockRequestCollection requestCollection)
        {
            this.eventDispatcher = eventDispatcher;
            this.requestCollection = requestCollection;

            On<PrefetchBlocksEvent>(HandlePrefetchBlocksEvent);
            On<RequestBlockEvent>(HandleRequestBlockEvent);
        }

        private void HandlePrefetchBlocksEvent(PrefetchBlocksEvent evt)
        {
            lock (monitor)
            {
                List<DbHeader> missingBlocks = new List<DbHeader>();
                foreach (DbHeader header in evt.Headers)
                {
                    if (blocks.TryGetValue(header.Hash, out var existingBlock))
                    {
                        blocks.Remove(header.Hash);
                        blocks.Add(header.Hash, existingBlock);
                    }
                    else
                    {
                        missingBlocks.Add(header);
                    }
                }

                requestCollection.AddRequest(evt.RequestOwner, missingBlocks);
                eventDispatcher.Raise(new BlockDownloadRequestedEvent());
            }
        }

        private void HandleRequestBlockEvent(RequestBlockEvent evt)
        {
            lock (monitor)
            {
                byte[] hash = evt.Hash;
                if (blocks.TryGetValue(hash, out var block))
                {
                    eventDispatcher.Raise(new BlockAvailableEvent(hash, block));
                }

                requestCollection.SetRequired(hash);
            }
        }

        public bool AddBlock(byte[] hash, BlockMessage block)
        {
            lock (monitor)
            {
                bool isNewBlock = !blocks.ContainsKey(hash);
                if (isNewBlock)
                {
                    blocks[hash] = block;
                }

                if (requestCollection.MarkReceived(hash))
                {
                    eventDispatcher.Raise(new BlockAvailableEvent(hash, block));
                }

                while (blocks.Count > MaxCachedBlocks)
                {
                    blocks.Remove(blocks.First().Key);
                }

                return isNewBlock;
            }
        }
    }
}