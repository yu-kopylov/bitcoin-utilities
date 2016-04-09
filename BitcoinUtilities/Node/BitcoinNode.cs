using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node
{
    /// <summary>
    /// Represents a bitcoin node that can interact with other nodes on the network.
    /// </summary>
    public class BitcoinNode : INotifyPropertyChanged
    {
        private readonly IBlockChainStorage storage;

        private NodeAddressCollection addressCollection;
        private NodeConnectionCollection connectionCollection;

        private BitcoinConnectionListener listener;
        private NodeDiscoveryThread nodeDiscoveryThread;


        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private bool started;

        public BitcoinNode(IBlockChainStorage storage)
        {
            this.storage = storage;
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
            addressCollection = new NodeAddressCollection();
            connectionCollection = new NodeConnectionCollection(cancellationTokenSource.Token);

            //todo: discover own external address? (it seems useless without external port)
            //todo: use setting to specify port and operating mode
            listener = new BitcoinConnectionListener(IPAddress.Any, 8333, HandleIncomingConnection);
            listener.Start();

            nodeDiscoveryThread = new NodeDiscoveryThread(this, cancellationTokenSource.Token);
            connectionCollection.Changed += nodeDiscoveryThread.Signal;

            Thread thread = new Thread(nodeDiscoveryThread.Run);
            thread.IsBackground = true;
            thread.Start();

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
            }
        }

        private void HandleIncomingConnection(BitcoinConnection connection)
        {
            BitcoinEndpoint endpoint = new BitcoinEndpoint(HandleMessage);
            endpoint.Connect(connection);

            if (!connectionCollection.Add(NodeConnectionDirection.Incoming, endpoint))
            {
                endpoint.Dispose();
            }
        }

        private bool HandleMessage(BitcoinEndpoint endpoint, IBitcoinMessage message)
        {
            if (message is AddrMessage)
            {
                SaveReceivedAddresses((AddrMessage) message);
                return true;
            }
            //todo: handle getaddr message
            //todo: implement
            return true;
        }

        public void Stop()
        {
            if (!Started)
            {
                return;
            }

            cancellationTokenSource.Cancel();

            listener?.Stop();

            //todo: wait for nodeDiscoveryThread to stop and dispose it

            Started = false;
        }

        private void SaveReceivedAddresses(AddrMessage addrMessage)
        {
            foreach (NetAddr addr in addrMessage.AddressList)
            {
                //todo: check timestamp in address?
                addressCollection.Add(new NodeAddress(addr.Address, addr.Port));
            }
        }
    }
}