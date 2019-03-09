using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Modules.Blocks
{
    public class BlockRequestProcessor : EventHandlingService
    {
        private readonly IEventDispatcher eventDispatcher;
        private readonly BlockRepository blockRepository;
        private readonly BlockDownloadRequestRepository requestRepository;

        private readonly Dictionary<object, byte[]> requestsByOwner = new Dictionary<object, byte[]>(ReferenceEqualityComparer<object>.Instance);

        public BlockRequestProcessor(IEventDispatcher eventDispatcher, BlockRepository blockRepository, BlockDownloadRequestRepository requestRepository)
        {
            this.eventDispatcher = eventDispatcher;
            this.blockRepository = blockRepository;
            this.requestRepository = requestRepository;

            On<PrefetchBlocksEvent>(HandlePrefetchBlocksEvent);
            On<RequestBlockEvent>(HandleRequestBlockEvent);
            On<BlockDownloadedEvent>(HandleBlockDownloadedEvent);
        }

        private void HandlePrefetchBlocksEvent(PrefetchBlocksEvent evt)
        {
            var missingBlocks = blockRepository.PrefetchBlocks(evt.Headers);
            requestRepository.AddRequest(evt.RequestOwner, missingBlocks);
        }

        private void HandleRequestBlockEvent(RequestBlockEvent evt)
        {
            byte[] hash = evt.Hash;
            BlockMessage block = blockRepository.GetBlock(hash);

            if (block != null)
            {
                eventDispatcher.Raise(new BlockAvailableEvent(hash, block));
                return;
            }

            requestsByOwner[evt.RequestOwner] = hash;
        }

        private void HandleBlockDownloadedEvent(BlockDownloadedEvent evt)
        {
            List<object> affectedRequestOwners = new List<object>();
            foreach (var pair in requestsByOwner)
            {
                if (pair.Value.SequenceEqual(evt.HeaderHash))
                {
                    affectedRequestOwners.Add(pair.Key);
                }
            }

            if (affectedRequestOwners.Any())
            {
                eventDispatcher.Raise(new BlockAvailableEvent(evt.HeaderHash, evt.Block));
                foreach (object owner in affectedRequestOwners)
                {
                    requestsByOwner.Remove(owner);
                }
            }
        }
    }
}