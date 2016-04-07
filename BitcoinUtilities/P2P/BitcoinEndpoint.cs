using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.Threading;
using NLog;

namespace BitcoinUtilities.P2P
{
    /// <summary>
    /// Provides a P2P Bitcoin network endpoint that listens for incoming messages and provides means for sending messages.
    /// </summary>
    /// <remarks>
    /// Specifications:<br/>
    /// https://en.bitcoin.it/wiki/Protocol_documentation <br/>
    /// https://bitcoin.org/en/developer-reference#p2p-network
    /// </remarks>
    public class BitcoinEndpoint : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private const string UserAgent = "/BitcoinUtilities:0.0.1/";
        private readonly int protocolVersion = 70002;
        private const ulong Services = 0;
        private const bool AcceptBroadcasts = false;
        private const int StartHeight = 0;
        private const ulong Nonce = 0; // todo: support Nonce

        private readonly BitcoinMessageHandler messageHandler;

        private const int MessageProcessingThreadsCount = 4;
        private const int MessageProcessingTasksCount = 5;
        private const int MessageProcessingStartTimeout = 300000;

        private BitcoinConnection conn;
        private BitcoinPeerInfo peerInfo;

        private Thread listenerThread;
        private BlockingThreadPool threadPool;
        private volatile bool running;

        public BitcoinEndpoint(BitcoinMessageHandler messageHandler)
        {
            this.messageHandler = messageHandler;
        }

        public void Dispose()
        {
            //todo: shutdown gracefully
            running = false;
            if (conn != null)
            {
                conn.Dispose();
            }
            if (threadPool != null)
            {
                //todo: set better value?
                threadPool.Stop(10000);
            }
        }

        public int ProtocolVersion
        {
            get { return protocolVersion; }
        }

        public BitcoinPeerInfo PeerInfo
        {
            get { return peerInfo; }
        }

        /// <summary>
        /// Connects to a remote host and sends a version handshake.
        /// </summary>
        /// <param name="host">The DNS name of the remote host.</param>
        /// <param name="port">The port number of the remote host.</param>
        /// <exception cref="BitcoinNetworkException">Connection failed.</exception>
        public void Connect(string host, int port)
        {
            if (conn != null)
            {
                throw new BitcoinNetworkException("Connection was already established.");
            }

            conn = new BitcoinConnection();
            conn.Connect(host, port);

            Start();
        }

        /// <summary>
        /// Connects to a remote host using an established connection and sends a version handshake.
        /// </summary>
        /// <param name="connection">The established network connection.</param>
        /// <exception cref="BitcoinNetworkException">Connection failed.</exception>
        public void Connect(BitcoinConnection connection)
        {
            if (conn != null)
            {
                throw new BitcoinNetworkException("Connection was already established.");
            }

            conn = connection;

            Start();
        }

        private void Start()
        {
            VersionMessage outVersionMessage = CreateVersionMessage();
            WriteMessage(outVersionMessage);

            BitcoinMessage incVersionMessage = conn.ReadMessage();
            if (incVersionMessage.Command != VersionMessage.Command)
            {
                throw new BitcoinNetworkException("Remote endpoint did not send Version message.");
            }

            VersionMessage incVersionMessageParsed;
            using (BitcoinStreamReader reader = new BitcoinStreamReader(new MemoryStream(incVersionMessage.Payload)))
            {
                //todo: review and handle exceptions
                incVersionMessageParsed = VersionMessage.Read(reader);
            }

            //todo: check minimal peer protocol version

            BitcoinMessage outVerAckMessage = new BitcoinMessage(BitcoinCommands.VerAck, new byte[0]);
            conn.WriteMessage(outVerAckMessage);

            BitcoinMessage incVerAckMessage = conn.ReadMessage();
            if (incVerAckMessage.Command != BitcoinCommands.VerAck)
            {
                //todo: handle correctly
                throw new BitcoinNetworkException("Remote endpoint did not send VerAck message.");
            }

            peerInfo = new BitcoinPeerInfo(conn.RemoteEndPoint, incVersionMessageParsed);

            running = true;

            threadPool = new BlockingThreadPool(MessageProcessingThreadsCount, MessageProcessingTasksCount);

            listenerThread = new Thread(Listen);
            listenerThread.Name = "Endpoint Listener";
            listenerThread.IsBackground = true; //todo: ??? should it be background?
            listenerThread.Start();
        }

        private void Listen()
        {
            while (running)
            {
                BitcoinMessage message;
                //todo: review exception handling
                try
                {
                    message = conn.ReadMessage();
                }
                catch (ObjectDisposedException)
                {
                    //todo: this happens when endpoint is disposed, can it happen for other reasons?
                    return;
                }
                catch (BitcoinNetworkException)
                {
                    //todo: this happens when endpoint is disposed, can it happen for other reasons?
                    return;
                }
                catch (IOException)
                {
                    //todo: this was happening before BitcoinNetworkException was introduced when endpoint was disposed, can it happen for other reasons?
                    return;
                }
                if (!threadPool.Execute(() => HandleMessage(message), MessageProcessingStartTimeout))
                {
                    //todo: terminate connection?
                    logger.Warn("Message was ignored because all threads were busy (command: {0}, payload: {1} byte(s)).", message.Command,
                        message.Payload.Length);
                }
            }
        }

        public void WriteMessage(IBitcoinMessage message)
        {
            BitcoinMessage rawMessage = new BitcoinMessage(message.Command, BitcoinStreamWriter.GetBytes(message.Write));
            conn.WriteMessage(rawMessage);
        }

        private void HandleMessage(BitcoinMessage message)
        {
            if (message.Command == BitcoinCommands.Ping)
            {
                if (message.Payload.Length != 8)
                {
                    conn.WriteMessage(CreateRejectMessage(message, BitcoinMessageRejectReason.Malformed,
                        "The Ping command payload should have a length of 8 bytes."));
                    return;
                }

                BitcoinMessage pongMessage = new BitcoinMessage(BitcoinCommands.Pong, message.Payload);
                conn.WriteMessage(pongMessage);
                return;
            }

            if (message.Command == BitcoinCommands.Reject)
            {
                //todo: stop endpoint / allow messageHandler to process it (be careful of reject message loop)?
                return;
            }

            IBitcoinMessage parsedMessage = BitcoinMessageParser.Parse(message);

            if (parsedMessage == null)
            {
                //todo: this is a misuse of the interface
                parsedMessage = message;
            }

            if (messageHandler == null || !messageHandler(this, parsedMessage))
            {
                conn.WriteMessage(CreateRejectMessage(message, BitcoinMessageRejectReason.Malformed, "Unknown command."));
            }
        }

        private BitcoinMessage CreateRejectMessage(BitcoinMessage originalMessage, BitcoinMessageRejectReason rejectReason, string rejectReasonText)
        {
            MemoryStream mem = new MemoryStream();

            BitcoinMessageUtils.AppendText(mem, originalMessage.Command);
            BitcoinMessageUtils.AppendByte(mem, (byte) rejectReason);
            BitcoinMessageUtils.AppendText(mem, rejectReasonText);

            return new BitcoinMessage(BitcoinCommands.Reject, mem.ToArray());
        }

        private VersionMessage CreateVersionMessage()
        {
            long timestamp = GetUnixTimestamp();
            IPEndPoint remoteEndPoint = conn.RemoteEndPoint;
            IPEndPoint localEndPoint = conn.LocalEndPoint; //todo: should it always be a public address

            VersionMessage message = new VersionMessage(
                UserAgent,
                protocolVersion,
                Services,
                timestamp,
                Nonce,
                localEndPoint,
                remoteEndPoint,
                StartHeight,
                AcceptBroadcasts);

            return message;
        }

        private long GetUnixTimestamp()
        {
            TimeSpan timeSpan = (DateTime.UtcNow - unixEpoch);
            return (long) timeSpan.TotalSeconds;
        }
    }

    /// <summary>
    /// Processes the message if it is supported.
    /// </summary>
    /// <param name="endpoint">The endpoint that received the message.</param>
    /// <param name="message">The message from the remote endpoint.</param>
    /// <returns>true if the given message is supported; otherwise, false.</returns>
    public delegate bool BitcoinMessageHandler(BitcoinEndpoint endpoint, IBitcoinMessage message);

    public enum BitcoinMessageRejectReason
    {
        Malformed = 0x01,
        Invalid = 0x10,
        Obsolete = 0x11,
        Duplicate = 0x12,
        Nonstandard = 0x40,
        Dust = 0x41,
        InsufficientFee = 0x42,
        Checkpoint = 0x43
    }

    public static class BitcoinCommands
    {
        public static readonly string VerAck = "verack";
        public static readonly string Ping = "ping";
        public static readonly string Pong = "pong";
        public static readonly string Reject = "reject";
    }
}