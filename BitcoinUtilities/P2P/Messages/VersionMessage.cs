using System.Net;

namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    /// When a node creates an outgoing connection, it will immediately advertise its version.
    /// The remote node will respond with its version.
    /// No further communication is possible until both peers have exchanged their version.
    /// </summary>
    public class VersionMessage : IBitcoinMessage
    {
        public const string Command = "version";

        private const int MaxUserAgentLength = 256 * 1024;

        public VersionMessage(
            string userAgent,
            int protocolVersion,
            BitcoinServiceFlags services,
            long timestamp,
            ulong nonce,
            IPEndPoint localEndpoint,
            IPEndPoint remoteEndpoint,
            int startHeight,
            bool acceptBroadcasts)
        {
            this.UserAgent = userAgent;
            this.ProtocolVersion = protocolVersion;
            this.Services = services;
            this.Timestamp = timestamp;
            this.Nonce = nonce;

            this.LocalEndpoint = localEndpoint;
            this.LocalServices = services;

            this.RemoteEndpoint = remoteEndpoint;
            this.RemoteServices = services;

            this.StartHeight = startHeight;
            this.AcceptBroadcasts = acceptBroadcasts;
        }

        string IBitcoinMessage.Command
        {
            get { return Command; }
        }

        /// <summary>
        /// Identifies protocol version being used by the node.
        /// </summary>
        public int ProtocolVersion { get; }

        /// <summary>
        /// Bitfield of features to be enabled for this connection.
        /// </summary>
        public BitcoinServiceFlags Services { get; }

        /// <summary>
        /// Standard UNIX timestamp in seconds.
        /// </summary>
        public long Timestamp { get; }

        /// <summary>
        /// The network address of the node emitting this message.
        /// </summary>
        public IPEndPoint LocalEndpoint { get; }

        /// <summary>
        /// Same services listed in <see cref="Services"/>.
        /// </summary>
        public BitcoinServiceFlags LocalServices { get; private set; }

        /// <summary>
        /// The network address of the node receiving this message.
        /// </summary>
        public IPEndPoint RemoteEndpoint { get; }

        /// <summary>
        /// Same services listed in <see cref="Services"/>.
        /// </summary>
        public BitcoinServiceFlags RemoteServices { get; private set; }

        /// <summary>
        /// A random nonce which can help a node to detect a connection to itself.
        /// <para/>
        /// Nonce is randomly generated every time a version packet is sent.
        /// <para/>
        /// If the nonce is 0, the nonce field is ignored.
        /// <para/>
        /// If the nonce is anything else, a node should terminate the connection on receipt of a version message with a nonce it previously sent.
        /// </summary>
        public ulong Nonce { get; }

        /// <summary>
        /// User Agent (described in BIP-14)
        /// </summary>
        public string UserAgent { get; }

        /// <summary>
        /// The last block received by the emitting node.
        /// </summary>
        public int StartHeight { get; }

        /// <summary>
        /// Whether the remote peer should announce relayed transactions or not (see BIP-37).
        /// </summary>
        public bool AcceptBroadcasts { get; }

        public void Write(BitcoinStreamWriter writer)
        {
            writer.Write(ProtocolVersion);
            writer.Write((ulong) Services);
            writer.Write(Timestamp);

            writer.Write((ulong) RemoteServices);
            writer.WriteAddress(RemoteEndpoint.Address);
            writer.WriteBigEndian((ushort) RemoteEndpoint.Port);

            writer.Write((ulong) LocalServices);
            writer.WriteAddress(LocalEndpoint.Address);
            writer.WriteBigEndian((ushort) LocalEndpoint.Port);

            writer.Write(Nonce);

            writer.WriteText(UserAgent);
            writer.Write(StartHeight);
            writer.Write(AcceptBroadcasts);
        }

        public static VersionMessage Read(BitcoinStreamReader reader)
        {
            int protocolVersion = reader.ReadInt32();
            BitcoinServiceFlags services = (BitcoinServiceFlags) reader.ReadUInt64();
            long timestamp = reader.ReadInt64();

            BitcoinServiceFlags remoteServices = (BitcoinServiceFlags) reader.ReadUInt64();
            IPAddress remoteAddress = reader.ReadAddress();
            ushort remotePort = reader.ReadUInt16BigEndian();
            IPEndPoint remoteEndpoint = new IPEndPoint(remoteAddress, remotePort);

            BitcoinServiceFlags localServices = (BitcoinServiceFlags) reader.ReadUInt64();
            IPAddress localAddress = reader.ReadAddress();
            ushort localPort = reader.ReadUInt16BigEndian();
            IPEndPoint localEndpoint = new IPEndPoint(localAddress, localPort);

            ulong nonce = reader.ReadUInt64();

            string userAgent = reader.ReadText(MaxUserAgentLength);
            int startHeight = reader.ReadInt32();
            //todo: support clients that don't send relay bit 
            bool acceptBroadcasts = reader.ReadBoolean();

            VersionMessage versionMessage = new VersionMessage(
                userAgent,
                protocolVersion,
                services,
                timestamp,
                nonce,
                localEndpoint,
                remoteEndpoint,
                startHeight,
                acceptBroadcasts);
            versionMessage.LocalServices = localServices;
            versionMessage.RemoteServices = remoteServices;
            return versionMessage;
        }
    }
}