using System;
using System.Threading;
using BitcoinUtilities.P2P;
using BitcoinUtilities.Storage;
using NLog;

namespace BitcoinUtilities.Node.Services
{
    public class BlockValidationService : INodeService
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly Blockchain blockchain;
        private readonly CancellationToken cancellationToken;

        private readonly AutoResetEvent resumeEvent = new AutoResetEvent(true);

        public BlockValidationService(Blockchain blockchain, CancellationToken cancellationToken)
        {
            this.blockchain = blockchain;
            this.cancellationToken = cancellationToken;

            cancellationToken.Register(() => resumeEvent.Set());
            //todo: StateChanged is wrong event, it does not reflect changes that does not affect BlockState structure
            blockchain.StateChanged += () => resumeEvent.Set();
        }

        public void Run()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!resumeEvent.WaitOne(TimeSpan.FromSeconds(60)))
                {
                    continue;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                UpdateState();
            }
        }

        private void UpdateState()
        {
            //todo: what if best header chain does not have content yet
            if (!blockchain.State.BestChain.IsInBestHeaderChain)
            {
                RevertBlockchain();
            }

            IncludeNextBlock();
        }

        private void IncludeNextBlock()
        {
            BlockSelector selector = new BlockSelector
            {
                IsInBestHeaderChain = true,
                Heights = new int[] {blockchain.State.BestChain.Height + 1},
                Order = BlockSelector.SortOrder.Height,
                Direction = BlockSelector.SortDirection.Asc
            };

            StoredBlock nextBlock = blockchain.FindFirst(selector);
            if (nextBlock != null && nextBlock.HasContent)
            {
                blockchain.Include(nextBlock.Hash);
                // this block can be not the last one available for inclusion
                resumeEvent.Set();
            }
        }

        private void RevertBlockchain()
        {
            BlockSelector selector = new BlockSelector();
            selector.IsInBestHeaderChain = true;
            selector.IsInBestBlockChain = true;
            selector.Order = BlockSelector.SortOrder.Height;
            selector.Direction = BlockSelector.SortDirection.Desc;
            StoredBlock lastBlockToKeep = blockchain.FindFirst(selector);

            //todo: revert block, restore mempool
            bool reverted;
            try
            {
                reverted = blockchain.TruncateTo(lastBlockToKeep.Hash);
            }
            catch (InvalidOperationException e)
            {
                // this exception should not happen, since only one thread updates blockchain
                logger.Error(e, $"Failed to revert blockchain to block '{BitConverter.ToString(lastBlockToKeep.Hash)}'.");
                return;
            }

            if (!reverted)
            {
                blockchain.Truncate();
            }
        }

        public void OnNodeConnected(BitcoinEndpoint endpoint)
        {
        }

        public void OnNodeDisconnected(BitcoinEndpoint endpoint)
        {
        }

        public void ProcessMessage(BitcoinEndpoint endpoint, IBitcoinMessage message)
        {
        }
    }
}