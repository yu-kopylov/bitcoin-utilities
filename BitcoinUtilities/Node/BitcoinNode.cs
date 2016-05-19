using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using BitcoinUtilities.Node.Services;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node
{
    /// <summary>
    /// Represents a bitcoin node that can interact with other nodes on the network.
    /// </summary>
    public class BitcoinNode : INotifyPropertyChanged, IDisposable
    {
        private readonly NodeServiceCollection services = new NodeServiceCollection();

        private readonly IBlockChainStorage storage;

        private NodeAddressCollection addressCollection;
        private NodeConnectionCollection connectionCollection;

        private BitcoinConnectionListener listener;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private bool started;

        public BitcoinNode(IBlockChainStorage storage)
        {
            this.storage = storage;

            services.AddFactory(new NodeDiscoveryServiceFactory());
            services.AddFactory(new BlockHeaderDownloadServiceFactory());
        }

        public void Dispose()
        {
            Stop();
        }

        public NodeAddressCollection AddressCollection
        {
            get { return addressCollection; }
        }

        public NodeConnectionCollection ConnectionCollection
        {
            get { return connectionCollection; }
        }

        public bool Started
        {
            get { return started; }
            set
            {
                //todo: is locks or invokes necessary here?
                started = value;
                OnPropertyChanged();
            }
        }

        public IBlockChainStorage Storage
        {
            get { return storage; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Start()
        {
            //todo: review the order in which services and listener start/stop, dispose created entities when start fails
            services.CreateServices(this, cancellationTokenSource.Token);

            addressCollection = new NodeAddressCollection();
            connectionCollection = new NodeConnectionCollection(cancellationTokenSource.Token);
            //todo: review, maybe registering event handler is a responsibility of nodeDiscoveryService 
            //todo: connectionCollection.Changed += nodeDiscoveryService.Signal; - signal nodeDiscoveryService to connect to new nodes (is it its responsibility?)

            //todo: discover own external address? (it seems useless without external port)
            //todo: use setting to specify port and operating mode
            listener = new BitcoinConnectionListener(IPAddress.Any, 8333, HandleIncomingConnection);
            listener.Start();

            services.Start();

            Started = true;
        }

        public void ConnectTo(NodeAddress address)
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            //todo: The construction of BitcoinEndpoint and BitcoinConnection is confusing. Both can use host and port.
            BitcoinEndpoint endpoint = new BitcoinEndpoint(HandleMessage);
            try
            {
                endpoint.Connect(address.Address.ToString(), address.Port);
            }
            catch (BitcoinNetworkException)
            {
                addressCollection.Reject(address);
                //todo: log error?
                return;
            }

            //todo: check nonce to avoid connection to self
            //todo: check if peer is banned
            //todo: send ping messages to check if connection is alive

            if ((endpoint.PeerInfo.VersionMessage.Services & VersionMessage.ServiceNodeNetwork) == 0)
            {
                // outgoing connections ignore non-full nodes
                addressCollection.Reject(address);
                endpoint.Dispose();
                return;
            }

            addressCollection.Confirm(address);

            if (!connectionCollection.Add(NodeConnectionDirection.Outgoing, endpoint))
            {
                endpoint.Dispose();
                return;
            }

            services.OnNodeConnected(endpoint);
        }

        private void HandleIncomingConnection(BitcoinConnection connection)
        {
            BitcoinEndpoint endpoint = new BitcoinEndpoint(HandleMessage);
            endpoint.Connect(connection);

            if (!connectionCollection.Add(NodeConnectionDirection.Incoming, endpoint))
            {
                endpoint.Dispose();
                return;
            }

            services.OnNodeConnected(endpoint);
        }

        private bool HandleMessage(BitcoinEndpoint endpoint, IBitcoinMessage message)
        {
            if (message is AddrMessage)
            {
                SaveReceivedAddresses((AddrMessage) message);
                return true;
            }
            //todo: send GetAddr message sometimes
            //todo: see 0.10.1 Change log: Ignore getaddr messages on Outbound connections.
            if (message is GetAddrMessage)
            {
                SendKnownAddresses(endpoint);
                return true;
            }

            services.ProcessMessage(endpoint, message);
            //todo: handle getaddr message
            //todo: implement
            return true;
        }

        public void Stop()
        {
            if (!Started)
            {
                throw new InvalidOperationException($"{nameof(BitcoinNode)} is not running.");
            }

            cancellationTokenSource.Cancel();

            listener?.Stop();

            //todo: use parameter for timeout?
            TimeSpan stopTimeout = TimeSpan.FromSeconds(30);
            bool servicesTerminated = services.Join(stopTimeout);
            //todo: use value of servicesTerminated

            services.DisposeServices();

            Started = false;
        }

        private void SaveReceivedAddresses(AddrMessage addrMessage)
        {
            foreach (NetAddr addr in addrMessage.AddressList)
            {
                //todo: check timestamp in address?
                //todo: prioritize connections to port 8333
                addressCollection.Add(new NodeAddress(addr.Address, addr.Port));
            }
        }

        private void SendKnownAddresses(BitcoinEndpoint endpoint)
        {
            List<NetAddr> addresses = new List<NetAddr>();
            List<NodeConnection> currentConnections = connectionCollection.GetConnections();
            foreach (NodeConnection connection in currentConnections)
            {
                //todo: filter loopback addresses
                NetAddr addr = new NetAddr(
                    //todo: use last message date instead
                    (uint) connection.Endpoint.PeerInfo.VersionMessage.Timestamp,
                    connection.Endpoint.PeerInfo.VersionMessage.Services,
                    endpoint.PeerInfo.IpEndpoint.Address,
                    (ushort) endpoint.PeerInfo.IpEndpoint.Port);
                addresses.Add(addr);
            }
            AddrMessage addrMessage = new AddrMessage(addresses.Take(AddrMessage.MaxAddressesPerMessage).ToArray());
            endpoint.WriteMessage(addrMessage);
        }
    }
}