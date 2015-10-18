using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

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
        private static readonly DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private const string UserAgent = "/BitcoinUtilities:0.0.1/";
        private readonly int protocolVersion = 70002;
        private const ulong Services = 0;
        private const bool AcceptBroadcasts = false;
        private const int StartHeight = 0;
        private const ulong Nonce = 0; // todo: support Nonce

        private readonly BitcoinMessageHandler messageHandler;

        private BitcoinConnection conn;
        private Thread thread;
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
        }

        public int ProtocolVersion
        {
            get { return protocolVersion; }
        }

        public void Connect(string host, int port)
        {
            if (conn != null)
            {
                throw new Exception("Connection is already established.");
            }

            conn = new BitcoinConnection();
            conn.Connect(host, port);

            Start();
        }

        public void Connect(BitcoinConnection connection)
        {
            if (conn != null)
            {
                throw new Exception("Connection is already established.");
            }

            conn = connection;

            Start();
        }

        private void Start()
        {
            BitcoinMessage outVersionMessage = CreateVersionMessage();
            conn.WriteMessage(outVersionMessage);

            BitcoinMessage incVersionMessage = conn.ReadMessage();
            if (incVersionMessage.Command != BitcoinCommands.Version)
            {
                //todo: handle correctly
                throw new Exception("Remote endpoind did not send Version message.");
            }

            //todo: check minimal peer protocol version

            BitcoinMessage outVerAckMessage = new BitcoinMessage(BitcoinCommands.VerAck, new byte[0]);
            conn.WriteMessage(outVerAckMessage);

            BitcoinMessage incVerAckMessage = conn.ReadMessage();
            conn.WriteMessage(outVerAckMessage);

            if (incVerAckMessage.Command != BitcoinCommands.VerAck)
            {
                //todo: handle correctly
                throw new Exception("Remote endpoind did not send VerAck message.");
            }

            running = true;

            thread = new Thread(Listen);
            thread.IsBackground = true; //todo: ??? should it be background?
            thread.Start();
        }

        private void Listen()
        {
            while (running)
            {
                BitcoinMessage message = conn.ReadMessage();
                HandleMessage(message);
            }
        }

        public void WriteMessage(IBitcoinMessage message)
        {
            MemoryStream mem = new MemoryStream();
            using (BitcoinStreamWriter writer = new BitcoinStreamWriter(mem))
            {
                message.Write(writer);
            }

            BitcoinMessage rawMessage = new BitcoinMessage(message.Command, mem.ToArray());

            //todo: add lock
            conn.WriteMessage(rawMessage);
        }

        private void HandleMessage(BitcoinMessage message)
        {
            if (message.Command == BitcoinCommands.Ping)
            {
                if (message.Payload.Length != 8)
                {
                    conn.WriteMessage(CreateRejectMessage(message, BitcoinMessageRejectReason.Malformed, "The Ping command payload should have a length of 8 bytes."));
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

        private BitcoinMessage CreateVersionMessage()
        {
            MemoryStream mem = new MemoryStream();

            BitcoinMessageUtils.AppendInt32LittleEndian(mem, protocolVersion);
            BitcoinMessageUtils.AppendUInt64LittleEndian(mem, Services);
            BitcoinMessageUtils.AppendInt64LittleEndian(mem, GetUnixTimestamp());

            BitcoinMessageUtils.AppendUInt64LittleEndian(mem, Services); // todo: what it should contain for remote address?
            IPEndPoint remoteEndPoint = conn.RemoteEndPoint;
            byte[] remoteAddress = remoteEndPoint.Address.MapToIPv6().GetAddressBytes();
            mem.Write(remoteAddress, 0, remoteAddress.Length);
            BitcoinMessageUtils.AppendUInt16BigEndian(mem, (ushort) remoteEndPoint.Port); //todo: shouldn't port be 8333 ?

            BitcoinMessageUtils.AppendUInt64LittleEndian(mem, Services);
            IPEndPoint localEndPoint = conn.LocalEndPoint; //todo: should it always a public address
            byte[] localAddress = localEndPoint.Address.MapToIPv6().GetAddressBytes();
            mem.Write(localAddress, 0, localAddress.Length);
            BitcoinMessageUtils.AppendUInt16BigEndian(mem, (ushort) localEndPoint.Port);

            BitcoinMessageUtils.AppendUInt64LittleEndian(mem, Nonce);

            BitcoinMessageUtils.AppendText(mem, UserAgent);
            BitcoinMessageUtils.AppendInt32LittleEndian(mem, StartHeight);
            BitcoinMessageUtils.AppendBool(mem, AcceptBroadcasts);

            return new BitcoinMessage(BitcoinCommands.Version, mem.ToArray());
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
        public static readonly string Version = "version";
        public static readonly string VerAck = "verack";
        public static readonly string Ping = "ping";
        public static readonly string Pong = "pong";
        public static readonly string Reject = "reject";
    }
}