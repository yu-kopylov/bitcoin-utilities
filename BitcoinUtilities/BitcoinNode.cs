using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using BitcoinUtilities.P2P;

namespace BitcoinUtilities
{
    /// <summary>
    /// Represents a bitcoin node that can interact with other nodes on the network.
    /// </summary>
    public class BitcoinNode : INotifyPropertyChanged
    {
        private readonly IBlockChainStorage storage;

        private BitcoinConnectionListener listener;

        private readonly List<BitcoinEndpoint> endpoints = new List<BitcoinEndpoint>();

        private bool started;

        public BitcoinNode(IBlockChainStorage storage)
        {
            this.storage = storage;
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
            //todo: discover own external address
            //todo: discover seeds
            //todo: use setting to specify port and operating mode
            listener = new BitcoinConnectionListener(IPAddress.Any, 8333, HandleConnection);
            listener.Start();
            Started = true;

            //todo: add persistent collection of known nodes
            List<IPAddress> nodeAddresses = DnsSeeds.GetNodeAddresses();
            if (nodeAddresses.Any())
            {
                Random random = new Random();
                for (int i = 0; i < 2; i++)
                {
                    //todo: The construction of BitcoinEndpoint and BitcoinConnection is confusing. Both can use host and port.
                    BitcoinEndpoint endpoint = new BitcoinEndpoint(HandleMessage);
                    //todo: describe and handle exceptions
                    //todo: don't connect to same node twice
                    IPAddress selectedAddress = nodeAddresses[random.Next(nodeAddresses.Count)];
                    endpoint.Connect(selectedAddress.ToString(), 8333);
                    endpoints.Add(endpoint);
                }
            }
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

            if (listener != null)
            {
                listener.Stop();
                listener = null;
            }

            foreach (BitcoinEndpoint endpoint in endpoints)
            {
                endpoint.Dispose();
            }

            endpoints.Clear();
            OnPropertyChanged(nameof(endpoints));

            Started = false;
        }
    }
}