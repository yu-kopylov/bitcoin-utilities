using System.Collections.Generic;
using BitcoinUtilities.P2P;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node
{
    /// <summary>
    /// An immutable object representing a connection to a node.
    /// </summary>
    public class NodeConnection
    {
        public NodeConnection(NodeConnectionDirection direction, BitcoinEndpoint endpoint, IEnumerable<IEventHandlingService> services)
        {
            Direction = direction;
            Endpoint = endpoint;
            Services = services == null ? null : new List<IEventHandlingService>(services);
        }

        public NodeConnectionDirection Direction { get; }

        public BitcoinEndpoint Endpoint { get; }

        public IReadOnlyCollection<IEventHandlingService> Services { get; }
    }
}