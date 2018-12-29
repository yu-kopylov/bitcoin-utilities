using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Storage;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Services
{
    public class HeaderDowloadServiceFactory : INodeEventServiceFactory
    {
        public IEventHandlingService CreateForEndpoint(BitcoinNode node, BitcoinEndpoint endpoint)
        {
            return new HeaderDowloadService(node, endpoint);
        }
    }

    public class HeaderDowloadService : NodeEventHandlingService
    {
        private readonly BitcoinNode node;
        private readonly BitcoinEndpoint endpoint;

        private readonly Dictionary<byte[], BlockHeader> unsavedHeaders = new Dictionary<byte[], BlockHeader>(ByteArrayComparer.Instance);

        public HeaderDowloadService(BitcoinNode node, BitcoinEndpoint endpoint)
        {
            this.node = node;
            this.endpoint = endpoint;
            OnMessage<HeadersMessage>(OnHeadersReceived);
        }

        public override void OnStart()
        {
            base.OnStart();
            BlockLocator blockLocator = node.Blockchain.GetBlockLocator();
            endpoint.WriteMessage(new GetHeadersMessage(endpoint.ProtocolVersion, blockLocator.GetHashes(), new byte[32]));
        }

        public void OnHeadersReceived(HeadersMessage message)
        {
            // todo: rewrite as follows
            // add headers to T
            // arrange headers in T
            // validate headers
            // save headers
            // locator = locator(T) + best head containing(T)

            foreach (var header in message.Headers)
            {
                // todo: we would not need write method if payload is passed with event
                byte[] hash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(header.Write));
                unsavedHeaders[hash] = header;
            }

            var storedHeaders = node.Blockchain.AddHeaders(message.Headers);

            // todo: can we use BlockLocator ?
            List<byte[]> blockLocator = new List<byte[]>();

            blockLocator.Add(node.Blockchain.State.BestHeader.Hash);

            foreach (var storedHeader in storedHeaders.Where(h => h.Height >= 0).OrderByDescending(h => h.Height))
            {
                if (blockLocator.Count < 32)
                {
                    blockLocator.Add(storedHeader.Hash);
                }

                // todo: also remove headers that stay in unsavedHeaders cache for too long
                unsavedHeaders.Remove(storedHeader.Hash);
            }

            endpoint.WriteMessage(new GetHeadersMessage(endpoint.ProtocolVersion, blockLocator.ToArray(), new byte[32]));
        }
    }
}