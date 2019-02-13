using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using BitcoinUtilities.Node.Components;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Node.Modules.Blocks;
using BitcoinUtilities.Node.Modules.Discovery;
using BitcoinUtilities.Node.Modules.Headers;
using BitcoinUtilities.Node.Modules.Outputs;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Threading;
using NLog;

namespace BitcoinUtilities.Node
{
    /// <summary>
    /// Represents a bitcoin node that can interact with other nodes on the network.
    /// </summary>
    public class BitcoinNode : INotifyPropertyChanged, IDisposable
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly List<INodeModule> modules = new List<INodeModule>();
        private readonly EventServiceController eventServiceController = new EventServiceController();
        private readonly NodeResourceCollection resources = new NodeResourceCollection();

        private readonly NetworkParameters networkParameters;
        private readonly string dataFolder;

        private readonly HeaderStorage headerStorage;
        private readonly Blockchain blockchain;

        private NodeAddressCollection addressCollection;
        private NodeConnectionCollection connectionCollection;

        private BitcoinConnectionListener connectionListener;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private bool started;

        public BitcoinNode(NetworkParameters networkParameters, string dataFolder)
        {
            this.networkParameters = networkParameters;
            this.dataFolder = dataFolder;

            Directory.CreateDirectory(dataFolder);

            // todo: storages are implementation-specific, should use some resource factory instead
            // todo: align creation of files and folders
            this.headerStorage = HeaderStorage.Open(Path.Combine(dataFolder, "headers.db"));
            this.blockchain = new Blockchain(headerStorage, networkParameters.GenesisBlock.BlockHeader);

            modules.Add(new NodeDiscoveryModule());
            modules.Add(new HeadersModule());
            modules.Add(new BlocksModule());
            modules.Add(new UtxoModule());
        }

        public void Dispose()
        {
            Stop();

            // todo: safely dispose all, even if one is failing
            headerStorage?.Dispose();
            cancellationTokenSource.Dispose();
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

        public NetworkParameters NetworkParameters
        {
            get { return networkParameters; }
        }

        public EventServiceController EventServiceController
        {
            get { return eventServiceController; }
        }

        public NodeResourceCollection Resources
        {
            get { return resources; }
        }

        public Blockchain Blockchain
        {
            get { return blockchain; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Start()
        {
            addressCollection = new NodeAddressCollection();
            connectionCollection = new NodeConnectionCollection();
            //todo: review, maybe registering event handler is a responsibility of the NodeDiscoveryService 
            connectionCollection.Changed += () => eventServiceController.Raise(new NodeConnectionsChangedEvent());

            //todo: discover own external address? (it seems useless without external port)
            //todo: use setting to specify port and operating mode
            connectionListener = BitcoinConnectionListener.StartListener(IPAddress.Any, 8333, networkParameters.NetworkMagic, HandleIncomingConnection);

            foreach (var module in modules)
            {
                module.CreateResources(this, dataFolder);
            }

            foreach (var module in modules)
            {
                foreach (var service in module.CreateNodeServices(this, cancellationTokenSource.Token))
                {
                    eventServiceController.AddService(service);
                }
            }

            eventServiceController.Start();

            Started = true;
        }

        public void Stop()
        {
            if (!Started)
            {
                throw new InvalidOperationException($"{nameof(BitcoinNode)} is not running.");
            }

            cancellationTokenSource.Cancel();

            connectionListener?.Dispose();

            connectionCollection?.Dispose();

            // todo: add timeout
            // todo: dispose IDisposable services in eventServiceController
            eventServiceController.Stop();

            resources.Dispose();

            Started = false;
        }

        public void ConnectTo(NodeAddress address)
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            BitcoinEndpoint endpoint;
            try
            {
                endpoint = BitcoinEndpoint.Create(address.Address.ToString(), address.Port, networkParameters.NetworkMagic);
            }
            catch (BitcoinNetworkException)
            {
                addressCollection.Reject(address);
                return;
            }

            //todo: check if peer is banned
            //todo: check nonce to avoid connection to self
            //todo: send ping messages to check if connection is alive

            if (!endpoint.PeerInfo.VersionMessage.Services.HasFlag(BitcoinServiceFlags.NodeNetwork))
            {
                // outgoing connections ignore non-full nodes
                addressCollection.Reject(address);
                endpoint.Dispose();
                return;
            }

            addressCollection.Confirm(address);
            SetupConnection(NodeConnectionDirection.Outgoing, endpoint);
        }

        private void HandleIncomingConnection(BitcoinConnection connection)
        {
            BitcoinEndpoint endpoint = BitcoinEndpoint.Create(connection);
            SetupConnection(NodeConnectionDirection.Incoming, endpoint);
        }

        private void SetupConnection(NodeConnectionDirection direction, BitcoinEndpoint endpoint)
        {
            if (!connectionCollection.Add(direction, endpoint))
            {
                endpoint.Dispose();
                return;
            }

            List<IEventHandlingService> endpointServices = new List<IEventHandlingService>();

            foreach (var module in modules)
            {
                foreach (var service in module.CreateEndpointServices(this, endpoint))
                {
                    eventServiceController.AddService(service);
                    endpointServices.Add(service);
                }
            }

            connectionCollection.AssignServices(endpoint, endpointServices);

            endpoint.StartListener(HandleMessage, HandleDisconnectedEndpoint);

            logger.Debug($"Started listener for endpoint '{endpoint.PeerInfo.IpEndpoint}'.");
        }

        private void HandleDisconnectedEndpoint(BitcoinEndpoint endpoint)
        {
            logger.Debug($"Endpoint '{endpoint.PeerInfo.IpEndpoint}' disconnected.");
            NodeConnection connection = connectionCollection.Remove(endpoint);
            if (connection?.Services != null)
            {
                foreach (IEventHandlingService service in connection.Services)
                {
                    // todo: dispose services if necessary
                    eventServiceController.RemoveService(service);
                }
            }

            logger.Debug($"Stopped {connection?.Services?.Count ?? 0} services for endpoint '{endpoint.PeerInfo.IpEndpoint}'.");
        }

        private void HandleMessage(BitcoinEndpoint endpoint, IBitcoinMessage message)
        {
            // todo: delegate message-handling to services

            if (message is AddrMessage addrMessage)
            {
                SaveReceivedAddresses(addrMessage);
                return;
            }

            //todo: send GetAddr message sometimes
            //todo: see 0.10.1 Change log: Ignore getaddr messages on Outbound connections.
            if (message is GetAddrMessage)
            {
                SendKnownAddresses(endpoint);
                return;
            }

            eventServiceController.Raise(new MessageEvent(endpoint, message));
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