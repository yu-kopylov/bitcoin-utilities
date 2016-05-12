using System.Threading;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;

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

        public void ProcessMessage(BitcoinEndpoint endpoint, IBitcoinMessage message)
        {
            HeadersMessage headersMessage = message as HeadersMessage;
            if (headersMessage != null)
            {
                ProcessHeadersMessage(headersMessage);
            }
        }

        private void ProcessHeadersMessage(HeadersMessage message)
        {
            //todo: check for exesting headers and calculate height for new headers
            node.Storage.AddHeaders(message.Headers);
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