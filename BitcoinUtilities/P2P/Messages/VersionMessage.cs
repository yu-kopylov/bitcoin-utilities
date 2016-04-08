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

        // NODE_NETWORK
        public const ulong ServiceNodeNetwork = 1;

        private const int MaxUserAgentLength = 256*1024;

        private readonly string userAgent;
        private readonly int protocolVersion;
        private readonly ulong services;

        private readonly long timestamp;
        private readonly ulong nonce;

        private readonly IPEndPoint localEndpoint;
        private ulong localServices;

        private readonly IPEndPoint remoteEndpoint;
        private ulong remoteServices;

        private readonly int startHeight;
        private readonly bool acceptBroadcasts;

        public VersionMessage(
            string userAgent,
            int protocolVersion,
            ulong services,
            long timestamp,
            ulong nonce,
            IPEndPoint localEndpoint,
            IPEndPoint remoteEndpoint,
            int startHeight,
            bool acceptBroadcasts)
        {
            this.userAgent = userAgent;
            this.protocolVersion = protocolVersion;
            this.services = services;
            this.timestamp = timestamp;
            this.nonce = nonce;

            this.localEndpoint = localEndpoint;
            this.localServices = services;

            this.remoteEndpoint = remoteEndpoint;
            this.remoteServices = services;

            this.startHeight = startHeight;
            this.acceptBroadcasts = acceptBroadcasts;
        }

        string IBitcoinMessage.Command
        {
            get { return Command; }
        }

        /// <summary>
        /// Identifies protocol version being used by the node.
        /// </summary>
        public int ProtocolVersion
        {
            get { return protocolVersion; }
        }

        /// <summary>
        /// Bitfield of features to be enabled for this connection.
        /// </summary>
        public ulong Services
        {
            get { return services; }
        }

        /// <summary>
        /// Standard UNIX timestamp in seconds.
        /// </summary>
        public long Timestamp
        {
            get { return timestamp; }
        }

        /// <summary>
        /// The network address of the node emitting this message.
        /// </summary>
        public IPEndPoint LocalEndpoint
        {
            get { return localEndpoint; }
        }

        /// <summary>
        /// Same services listed in <see cref="Services"/>.
        /// </summary>
        public ulong LocalServices
        {
            get { return localServices; }
            private set { localServices = value; }
        }

        /// <summary>
        /// The network address of the node receiving this message.
        /// </summary>
        public IPEndPoint RemoteEndpoint
        {
            get { return remoteEndpoint; }
        }

        /// <summary>
        /// Same services listed in <see cref="Services"/>.
        /// </summary>
        public ulong RemoteServices
        {
            get { return remoteServices; }
            private set { remoteServices = value; }
        }

        /// <summary>
        /// A random nonce which can help a node detect a connection to itself.
        /// <para/>
        /// Nonce is randomly generated every time a version packet is sent.
        /// <para/>
        /// If the nonce is 0, the nonce field is ignored.
        /// <para/>
        /// If the nonce is anything else, a node should terminate the connection on receipt of a version message with a nonce it previously sent.
        /// </summary>
        public ulong Nonce
        {
            get { return nonce; }
        }

        /// <summary>
        /// User Agent (described in BIP-14)
        /// </summary>
        public string UserAgent
        {
            get { return userAgent; }
        }

        /// <summary>
        /// The last block received by the emitting node.
        /// </summary>
        public int StartHeight
        {
            get { return startHeight; }
        }

        /// <summary>
        /// Whether the remote peer should announce relayed transactions or not (see BIP-37).
        /// </summary>
        public bool AcceptBroadcasts
        {
            get { return acceptBroadcasts; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
            writer.Write(protocolVersion);
            writer.Write(services);
            writer.Write(timestamp);

            writer.Write(remoteServices);
            writer.WriteAddress(remoteEndpoint.Address);
            writer.WriteBigEndian((ushort) remoteEndpoint.Port);

            writer.Write(localServices);
            writer.WriteAddress(localEndpoint.Address);
            writer.WriteBigEndian((ushort) localEndpoint.Port);

            writer.Write(nonce);

            writer.WriteText(UserAgent);
            writer.Write(startHeight);
            writer.Write(acceptBroadcasts);
        }

        public static VersionMessage Read(BitcoinStreamReader reader)
        {
            int protocolVersion = reader.ReadInt32();
            ulong services = reader.ReadUInt64();
            long timestamp = reader.ReadInt64();

            ulong remoteServices = reader.ReadUInt64();
            IPAddress remoteAddress = reader.ReadAddress();
            ushort remotePort = reader.ReadUInt16BigEndian();
            IPEndPoint remoteEndpoint = new IPEndPoint(remoteAddress, remotePort);

            ulong localServices = reader.ReadUInt64();
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