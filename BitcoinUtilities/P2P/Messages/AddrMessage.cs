using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    /// Provide information on known nodes of the network.
    /// <para/>
    /// Non-advertised nodes should be forgotten after typically 3 hours.
    /// <para/>
    /// Starting version 31402, addresses are prefixed with a timestamp.
    /// If no timestamp is present, the addresses should not be relayed to other peers, unless it is indeed confirmed they are up.
    /// </summary>
    public class AddrMessage : IBitcoinMessage
    {
        public const string Command = "addr";

        public const int MaxAddressesPerMessage = 1000;

        private readonly NetAddr[] addressList;

        public AddrMessage(NetAddr[] addressList)
        {
            this.addressList = addressList;
        }

        /// <summary>
        /// IP address entries. Up to a maximum of <see cref="MaxAddressesPerMessage"/>.
        /// </summary>
        public NetAddr[] AddressList
        {
            get { return addressList; }
        }

        string IBitcoinMessage.Command
        {
            get { return Command; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
            writer.WriteArray(addressList, (w, a) => a.Write(w));
        }

        public static AddrMessage Read(BitcoinStreamReader reader)
        {
            NetAddr[] addressList = reader.ReadArray(MaxAddressesPerMessage, NetAddr.Read);
            return new AddrMessage(addressList);
        }
    }
}