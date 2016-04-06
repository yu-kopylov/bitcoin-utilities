using System.Net;

namespace BitcoinUtilities.Node
{
    public class NodeAddress
    {
        private readonly IPAddress address;
        private readonly int port;

        public NodeAddress(IPAddress address, int port)
        {
            this.address = address;
            this.port = port;
        }

        public IPAddress Address
        {
            get { return address; }
        }

        public int Port
        {
            get { return port; }
        }

        protected bool Equals(NodeAddress other)
        {
            return Equals(address, other.address) && port == other.port;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NodeAddress) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((address != null ? address.GetHashCode() : 0)*397) ^ port;
            }
        }
    }
}