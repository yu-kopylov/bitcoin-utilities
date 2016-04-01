using System.ComponentModel;
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
        }

        private void HandleConnection(BitcoinConnection connection)
        {
            //todo: simplify disposing
            using (connection)
            {
                using (BitcoinEndpoint endpoint = new BitcoinEndpoint(HandleMessage))
                {
                    endpoint.Connect(connection);
                }
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

            //todo: terminate exesting endpoints

            Started = false;
        }
    }
}