using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;

namespace BitcoinUtilities.Node
{
    /// <summary>
    /// Represents a bitcoin node that can interact with other nodes on the network.
    /// </summary>
    public class BitcoinNode : INotifyPropertyChanged
    {
        private readonly IBlockChainStorage storage;

        private NodeAddressCollection addressCollection;

        private BitcoinConnectionListener listener;
        private NodeDiscoveryThread nodeDiscoveryThread;

        private readonly List<BitcoinEndpoint> endpoints = new List<BitcoinEndpoint>();

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

        public List<BitcoinEndpoint> Endpoints
        {
            get { return endpoints; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Start()
        {
            addressCollection = new NodeAddressCollection();

            //todo: discover own external address? (it seems useless without external port)
            //todo: use setting to specify port and operating mode
            listener = new BitcoinConnectionListener(IPAddress.Any, 8333, HandleConnection);
            listener.Start();

            nodeDiscoveryThread = new NodeDiscoveryThread(this, cancellationTokenSource.Token);

            Thread thread = new Thread(nodeDiscoveryThread.Run);
            thread.IsBackground = true;
            thread.Start();

            Started = true;
        }

        public void ConnectTo(NodeAddress address)
        {
            //todo: The construction of BitcoinEndpoint and BitcoinConnection is confusing. Both can use host and port.
            BitcoinEndpoint endpoint = new BitcoinEndpoint(HandleMessage);
            //todo: describe and handle exceptions
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

            //todo: there is a race condition here between Stop command and adding endpoints
            if (cancellationTokenSource.IsCancellationRequested)
            {
                endpoint.Dispose();
                return;
            }

            if ((endpoint.PeerInfo.VersionMessage.Services & VersionMessage.ServiceNodeNetwork) == 0)
            {
                // outgoing connections ignore non-full nodes
                addressCollection.Reject(address);
                endpoint.Dispose();
                return;
            }

            //todo: check if remote node is an SPV
            addressCollection.Confirm(address);

            endpoints.Add(endpoint);
            OnPropertyChanged(nameof(endpoints));
        }

        private void HandleConnection(BitcoinConnection connection)
        {
            //todo: simplify disposing
            using (connection)
            {
                BitcoinEndpoint endpoint = new BitcoinEndpoint(HandleMessage);
                endpoint.Connect(connection);
                /*
                    todo: Limit the number of cuncurrent connections and make sure that some was initiated by us
                    todo: to avoid monopolization of connection pool by unfriendly set of nodes.
                */
                endpoints.Add(endpoint);
                OnPropertyChanged(nameof(endpoints));
            }
        }

        private bool HandleMessage(BitcoinEndpoint endpoint, IBitcoinMessage message)
        {
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

            if (listener != null)
            {
                listener.Stop();
            }

            foreach (BitcoinEndpoint endpoint in endpoints)
            {
                endpoint.Dispose();
            }

            //todo: wait for nodeDiscoveryThread to stop and dispose it

            //todo: instead remove endpoints on disconnection
            endpoints.Clear();
            OnPropertyChanged(nameof(endpoints));

            Started = false;
        }
    }
}