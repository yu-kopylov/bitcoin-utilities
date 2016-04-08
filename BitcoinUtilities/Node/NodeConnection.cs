using BitcoinUtilities.P2P;

namespace BitcoinUtilities.Node
{
    public class NodeConnection
    {
        private readonly NodeConnectionDirection direction;
        private readonly BitcoinEndpoint endpoint;

        public NodeConnection(NodeConnectionDirection direction, BitcoinEndpoint endpoint)
        {
            this.direction = direction;
            this.endpoint = endpoint;
        }

        public NodeConnectionDirection Direction
        {
            get { return direction; }
        }

        public BitcoinEndpoint Endpoint
        {
            get { return endpoint; }
        }
    }
}