using System.Collections.Generic;
using System.Threading;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.Storage;

namespace BitcoinUtilities.Node.Services
{
    public class BlockHeaderDownloadService : INodeService
    {
        private readonly BitcoinNode node;

        public BlockHeaderDownloadService(BitcoinNode node, CancellationToken cancellationToken)
        {
            this.node = node;
        }

        public void Run()
        {
            //todo: implement
        }

        public void OnNodeConnected(BitcoinEndpoint endpoint)
        {
            BlockLocator blockLocator = node.Blockchain.GetBlockLocator();
            //todo: check if remote node provides this service?
            RequestHeaders(endpoint, blockLocator);
        }

        public void ProcessMessage(BitcoinEndpoint endpoint, IBitcoinMessage message)
        {
            HeadersMessage headersMessage = message as HeadersMessage;
            if (headersMessage != null)
            {
                ProcessHeadersMessage(endpoint, headersMessage);
            }
        }

        private void ProcessHeadersMessage(BitcoinEndpoint endpoint, HeadersMessage message)
        {
            List<StoredBlock> storedBlocks = node.Blockchain.AddHeaders(message.Headers);

            //todo: save blockLocator per node between requests?
            BlockLocator blockLocator = new BlockLocator();

            //todo: what should happen if message contains headers from different branchas?
            bool hasSavedHeaders = false;
            foreach (StoredBlock block in storedBlocks)
            {
                if (block.Height >= 0)
                {
                    //todo: this code assumes that blocks in storedBlocks collections are in order of ascending height and belong to one branch
                    blockLocator.AddHash(block.Height, block.Hash);
                    hasSavedHeaders = true;
                }
            }

            //todo: review this condition
            if (hasSavedHeaders)
            {
                RequestHeaders(endpoint, blockLocator);
            }
        }

        private static void RequestHeaders(BitcoinEndpoint endpoint, BlockLocator blockLocator)
        {
            endpoint.WriteMessage(new GetHeadersMessage(endpoint.ProtocolVersion, blockLocator.GetHashes(), new byte[32]));
        }
    }

    public class BlockHeaderDownloadServiceFactory : INodeServiceFactory
    {
        public INodeService Create(BitcoinNode node, CancellationToken cancellationToken)
        {
            return new BlockHeaderDownloadService(node, cancellationToken);
        }
    }
}