using System.Threading;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;

namespace BitcoinUtilities.Node.Services
{
    public class BlockHeaderDownloadService : INodeService
    {
        private BitcoinNode node;

        public void Init(BitcoinNode node, CancellationToken cancellationToken)
        {
            this.node = node;
        }

        public void Run()
        {
            //todo: implement
        }

        public void ProcessMessage(IBitcoinMessage message)
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
}