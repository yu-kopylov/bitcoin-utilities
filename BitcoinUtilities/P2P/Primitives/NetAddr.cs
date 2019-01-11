using System.Net;

namespace BitcoinUtilities.P2P.Primitives
{
    /// <summary>
    /// When a network address is needed somewhere, this structure is used.
    /// Network addresses are not prefixed with a timestamp in the version message.
    /// </summary>
    public struct NetAddr
    {
        public NetAddr(uint timestamp, BitcoinServiceFlags services, IPAddress address, ushort port)
        {
            this.Timestamp = timestamp;
            this.Services = services;
            this.Address = address;
            this.Port = port;
        }

        /// <summary>
        /// A time in Unix epoch time format.
        /// <para/>
        /// Nodes advertising their own IP address set this to the current time.
        /// <para/>
        /// Nodes advertising IP addresses they’ve connected to set this to the last time they connected to that node.
        /// <para/>
        /// Other nodes just relaying the IP address should not change the time.
        /// <para/>
        /// Nodes can use the time field to avoid relaying old addr messages. 
        /// </summary>
        /// <remark>
        /// Added in protocol version 31402. 
        /// </remark>
        public uint Timestamp { get; }

        /// <summary>
        /// The services the node advertised in its version message.
        /// </summary>
        public BitcoinServiceFlags Services { get; }

        /// <summary>
        /// IPv6 address in big endian byte order.
        /// IPv4 addresses can be provided as IPv4-mapped IPv6 addresses.
        /// </summary>
        public IPAddress Address { get; }

        /// <summary>
        /// Port number in big endian byte order.
        /// <para/>
        /// Note that Bitcoin Core will only connect to nodes with non-standard port numbers as a last resort for finding peers.
        /// This is to prevent anyone from trying to use the network to disrupt non-Bitcoin services that run on other ports.
        /// </summary>
        public ushort Port { get; }

        public void Write(BitcoinStreamWriter writer)
        {
            writer.Write(Timestamp);
            writer.Write((ulong) Services);
            writer.WriteAddress(Address);
            writer.WriteBigEndian(Port);
        }

        public static NetAddr Read(BitcoinStreamReader reader)
        {
            uint timestamp = reader.ReadUInt32();
            BitcoinServiceFlags services = (BitcoinServiceFlags) reader.ReadUInt64();
            IPAddress address = reader.ReadAddress();
            ushort port = reader.ReadUInt16BigEndian();
            return new NetAddr(timestamp, services, address, port);
        }
    }
}