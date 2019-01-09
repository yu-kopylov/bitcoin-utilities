using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Node.Services;
using BitcoinUtilities.Node.Services.Blocks;
using BitcoinUtilities.Node.Services.Headers;
using BitcoinUtilities.Node.Services.Outputs;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Storage;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node
{
    /// <summary>
    /// Represents a bitcoin node that can interact with other nodes on the network.
    /// </summary>
    public class BitcoinNode : INotifyPropertyChanged, IDisposable
    {
        private readonly NodeServiceCollection services = new NodeServiceCollection();
        private readonly List<INodeEventServiceFactory> eventServiceFactories = new List<INodeEventServiceFactory>();

        private readonly NetworkParameters networkParameters;
        private readonly IBlockchainStorage storage;
        private readonly Blockchain blockchain;

        private readonly HeaderStorage headerStorage;
        private readonly Blockchain2 blockchain2;
        private readonly BlockStorage blockStorage;
        private readonly UtxoStorage utxoStorage;

        private readonly EventServiceController eventServiceController = new EventServiceController();
        private NodeAddressCollection addressCollection;
        private NodeConnectionCollection connectionCollection;

        private BitcoinConnectionListener listener;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private bool started;

        public BitcoinNode(NetworkParameters networkParameters, string dataFolder, IBlockchainStorage storage)
        {
            this.networkParameters = networkParameters;
            this.storage = storage;
            this.blockchain = new Blockchain(networkParameters, storage);

            // todo: storages are implementation-specific, should use some resource factory instead
            // todo: align creation of files and folders
            this.headerStorage = HeaderStorage.Open(Path.Combine(dataFolder, "headers.db"));
            this.blockchain2 = new Blockchain2(headerStorage, networkParameters.GenesisBlock.BlockHeader);
            this.blockStorage = BlockStorage.Open(dataFolder);
            this.utxoStorage = UtxoStorage.Open(Path.Combine(dataFolder, "utxo2.db"));

            // todo: this should not be in BitcoinNode (should be handled by block storage)
            // todo: check for existance inside AddBlock
            byte[] genesisBlockHash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(networkParameters.GenesisBlock.BlockHeader.Write));
            if (blockStorage.GetBlock(genesisBlockHash) == null)
            {
                this.blockStorage.AddBlock(
                    genesisBlockHash,
                    BitcoinStreamWriter.GetBytes(networkParameters.GenesisBlock.Write)
                );
            }

            services.AddFactory(new NodeDiscoveryServiceFactory());
            //services.AddFactory(new BlockHeaderDownloadServiceFactory());
            //services.AddFactory(new BlockContentDownloadServiceFactory());
            //services.AddFactory(new BlockValidationServiceFactory());
            eventServiceFactories.Add(new HeaderDowloadServiceFactory());
            eventServiceFactories.Add(new BlockStorageServiceFactory());
            eventServiceFactories.Add(new BlockDownloadServiceFactory());
            eventServiceFactories.Add(new UtxoUpdateServiceFactory());
        }

        public void Dispose()
        {
            Stop();

            // todo: safely dispose all, even if one is failing
            headerStorage?.Dispose();
            blockStorage?.Dispose();
            utxoStorage?.Dispose();
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

        public IBlockchainStorage Storage
        {
            get { return storage; }
        }

        public Blockchain Blockchain
        {
            get { return blockchain; }
        }

        public Blockchain2 Blockchain2
        {
            get { return blockchain2; }
        }

        public BlockStorage BlockStorage
        {
            get { return blockStorage; }
        }

        public UtxoStorage UtxoStorage
        {
            get { return utxoStorage; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Start()
        {
            blockchain.Init();

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

            foreach (var factory in eventServiceFactories)
            {
                foreach (var service in factory.CreateForNode(this))
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

            listener?.Stop();

            //todo: use parameter for timeout?
            TimeSpan stopTimeout = TimeSpan.FromSeconds(30);
            bool servicesTerminated = services.Join(stopTimeout);
            //todo: use value of servicesTerminated

            // todo: add timeout
            // todo: remove and dispose if necessary services from eventServiceController
            eventServiceController.Stop();

            services.DisposeServices();

            Started = false;
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

            // todo: have to guarantee that no message is processed before service is added
            foreach (var factory in eventServiceFactories)
            {
                foreach (var service in factory.CreateForEndpoint(this, endpoint))
                {
                    eventServiceController.AddService(service);
                }
            }

            services.OnNodeConnected(endpoint);
            //todo: make sure that OnNodeConnected is always called before OnNodeDisconnected and before message processing
            //todo: remove associated services from eventServiceController
            endpoint.Disconnected += () => services.OnNodeDisconnected(endpoint);
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

            // todo: have to guarantee that no message is processed before service is added
            foreach (var factory in eventServiceFactories)
            {
                foreach (var service in factory.CreateForEndpoint(this, endpoint))
                {
                    eventServiceController.AddService(service);
                }
            }

            services.OnNodeConnected(endpoint);
            //todo: make sure that OnNodeConnected is always called before OnNodeDisconnected and before message processing
            //todo: remove associated services from eventServiceController
            endpoint.Disconnected += () => services.OnNodeDisconnected(endpoint);
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
            eventServiceController.Raise(new MessageEvent(endpoint, message));
            //todo: handle getaddr message
            //todo: implement
            return true;
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