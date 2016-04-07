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
        /// Node random nonce, randomly generated every time a version packet is sent.
        /// <para/>
        /// This nonce is used to detect connections to self.
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
            WriteAddress(writer, remoteEndpoint);

            writer.Write(localServices);
            WriteAddress(writer, localEndpoint);

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
            IPEndPoint remoteEndpoint = ReadAddress(reader);

            ulong localServices = reader.ReadUInt64();
            IPEndPoint localEndpoint = ReadAddress(reader);

            ulong nonce = reader.ReadUInt64();

            string userAgent = reader.ReadText(MaxUserAgentLength);
            int startHeight = reader.ReadInt32();
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

        private static void WriteAddress(BitcoinStreamWriter writer, IPEndPoint endpoint)
        {
            byte[] addressBytes = endpoint.Address.MapToIPv6().GetAddressBytes();
            writer.Write(addressBytes, 0, addressBytes.Length);

            writer.WriteBigEndian((ushort) endpoint.Port);
        }

        private static IPEndPoint ReadAddress(BitcoinStreamReader reader)
        {
            byte[] addressBytes = reader.ReadBytes(16);
            IPAddress remoteAddress = new IPAddress(addressBytes);

            int port = reader.ReadUInt16BigEndian();

            return new IPEndPoint(remoteAddress, port);
        }
    }
}