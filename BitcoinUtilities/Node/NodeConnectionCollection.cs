using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using BitcoinUtilities.P2P;

namespace BitcoinUtilities.Node
{
    //todo: write tests
    /// <summary>
    /// A thread-safe collection of connections to Bitcoin nodes.
    /// </summary>
    public class NodeConnectionCollection
    {
        private readonly CancellationToken cancellationToken;

        private readonly object lockObject = new object();

        private readonly List<NodeConnection> connections = new List<NodeConnection>();

        private int maxIncomingConnectionsCount = 8;
        private int maxOutgoingConnectionsCount = 8;

        private int incomingConnectionsCount;
        private int outgoingConnectionsCount;

        public NodeConnectionCollection(CancellationToken cancellationToken)
        {
            cancellationToken.Register(CloseConnections);
            this.cancellationToken = cancellationToken;
        }

        public int MaxIncomingConnectionsCount
        {
            get
            {
                lock (lockObject)
                {
                    return maxIncomingConnectionsCount;
                }
            }
            set
            {
                lock (lockObject)
                {
                    maxIncomingConnectionsCount = value;
                }
            }
        }

        public int MaxOutgoingConnectionsCount
        {
            get
            {
                lock (lockObject)
                {
                    return maxOutgoingConnectionsCount;
                }
            }
            set
            {
                lock (lockObject)
                {
                    maxOutgoingConnectionsCount = value;
                }
            }
        }

        public int IncomingConnectionsCount
        {
            get
            {
                lock (lockObject)
                {
                    return incomingConnectionsCount;
                }
            }
        }

        public int OutgoingConnectionsCount
        {
            get
            {
                lock (lockObject)
                {
                    return outgoingConnectionsCount;
                }
            }
        }

        public event Action Changed;

        /// <summary>
        /// Returns a list of existing connections.
        /// </summary>
        /// <returns></returns>
        public List<NodeConnection> GetConnections()
        {
            lock (lockObject)
            {
                return new List<NodeConnection>(connections);
            }
        }

        //todo: may be it is a little bit crude (port, IP version and direction are ignored)
        public bool IsConnected(IPAddress address)
        {
            lock (lockObject)
            {
                return connections.Any(c => c.Endpoint.PeerInfo.IpEndpoint.Address.Equals(address));
            }
        }

        /// <summary>
        /// Adds a connection to the collection.
        /// <para/>
        /// Can reject the connection if the cancellation token was cancelled or the maximum number of connections is reached.
        /// </summary>
        /// <param name="direction">The direction of the connection.</param>
        /// <param name="endpoint">The connection to a Bitcoin node.</param>
        /// <returns>true if the connection was accepted; otherwise false.</returns>
        public bool Add(NodeConnectionDirection direction, BitcoinEndpoint endpoint)
        {
            NodeConnection connection = new NodeConnection(direction, endpoint);

            lock (lockObject)
            {
                if (!CanRegister(connection))
                {
                    return false;
                }

                if (direction == NodeConnectionDirection.Incoming)
                {
                    incomingConnectionsCount++;
                }
                else if (direction == NodeConnectionDirection.Outgoing)
                {
                    outgoingConnectionsCount++;
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected connection direction: {direction}.");
                }

                connections.Add(connection);
            }

            //todo: there is a race condition here (endpoint can disconnect before handler is registered)
            endpoint.Disconnected += () => Remove(connection);

            Changed?.Invoke();

            return true;
        }

        private bool CanRegister(NodeConnection connection)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            if (connection.Direction == NodeConnectionDirection.Incoming)
            {
                if (incomingConnectionsCount >= maxIncomingConnectionsCount)
                {
                    return false;
                }
                //todo: should IsConnected be checked for incoming connections?
            }
            else if (connection.Direction == NodeConnectionDirection.Outgoing)
            {
                if (outgoingConnectionsCount >= maxOutgoingConnectionsCount)
                {
                    return false;
                }
                if (IsConnected(connection.Endpoint.PeerInfo.IpEndpoint.Address))
                {
                    return false;
                }
            }
            else
            {
                throw new InvalidOperationException($"Unexpected connection direction: {connection.Direction}.");
            }
            return true;
        }

        private void Remove(NodeConnection connection)
        {
            lock (lockObject)
            {
                if (!connections.Remove(connection))
                {
                    return;
                }
                if (connection.Direction == NodeConnectionDirection.Incoming)
                {
                    incomingConnectionsCount--;
                }
                else if (connection.Direction == NodeConnectionDirection.Outgoing)
                {
                    outgoingConnectionsCount--;
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected connection direction: {connection.Direction}.");
                }
            }
        }

        private void CloseConnections()
        {
            List<NodeConnection> connectionsToClose;

            lock (lockObject)
            {
                connectionsToClose = new List<NodeConnection>(connections);
            }

            foreach (NodeConnection connection in connectionsToClose)
            {
                connection.Endpoint.Dispose();
            }
        }
    }
}