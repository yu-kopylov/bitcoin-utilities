using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BitcoinUtilities.P2P;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node
{
    //todo: write tests
    /// <summary>
    /// A thread-safe collection of connections to a node.
    /// </summary>
    public class NodeConnectionCollection : IDisposable
    {
        private readonly object lockObject = new object();

        private readonly Dictionary<BitcoinEndpoint, NodeConnection> connections = new Dictionary<BitcoinEndpoint, NodeConnection>();

        private int maxIncomingConnectionsCount = 8;
        private int maxOutgoingConnectionsCount = 8;

        private int incomingConnectionsCount;
        private int outgoingConnectionsCount;

        private volatile bool disposed;

        /// <summary>
        /// Stops collection from accepting new connections and closes all associated endpoints.
        /// </summary>
        /// <remarks>
        /// This method does not remove connections from this collection.
        /// However, since endpoints are closed, their associated message listener threads will call their disconnect handlers
        /// which should remove connections from this collection.
        /// </remarks>
        public void Dispose()
        {
            List<BitcoinEndpoint> endpoints;

            lock (lockObject)
            {
                disposed = true;
                endpoints = new List<BitcoinEndpoint>(connections.Keys);
            }

            foreach (BitcoinEndpoint endpoint in endpoints)
            {
                endpoint.Dispose();
            }
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
                return new List<NodeConnection>(connections.Values);
            }
        }

        //todo: may be it is a little bit crude (port, IP version and direction are ignored)
        public bool IsConnected(IPAddress address)
        {
            lock (lockObject)
            {
                return connections.Keys.Any(e => e.PeerInfo.IpEndpoint.Address.Equals(address));
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
            NodeConnection connection = new NodeConnection(direction, endpoint, null);

            lock (lockObject)
            {
                if (!CanRegisterUnsafe(connection))
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

                connections.Add(endpoint, connection);
            }

            Changed?.Invoke();

            return true;
        }

        public void AssingServices(BitcoinEndpoint endpoint, IEnumerable<IEventHandlingService> endpointServices)
        {
            lock (lockObject)
            {
                var connection = connections[endpoint];
                if (connection.Services != null)
                {
                    throw new InvalidOperationException("Connection already have associated services.");
                }

                connections[endpoint] = new NodeConnection(connection.Direction, connection.Endpoint, endpointServices);
            }
        }

        private bool CanRegisterUnsafe(NodeConnection connection)
        {
            if (disposed)
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

        public NodeConnection Remove(BitcoinEndpoint endpoint)
        {
            NodeConnection connection;
            lock (lockObject)
            {
                if (!connections.TryGetValue(endpoint, out connection))
                {
                    return null;
                }

                connections.Remove(endpoint);

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

            Changed?.Invoke();
            return connection;
        }
    }
}