using System;
using System.IO;
using System.Net;
using System.Threading;
using BitcoinUtilities.P2P.Messages;
using NLog;

namespace BitcoinUtilities.P2P
{
    /// <summary>
    /// Represents a P2P Bitcoin network endpoint.
    /// <list type="bullet">
    /// <item><description>Has a thread that listens for incoming messages.</description></item>
    /// <item><description>Answers ping messages.</description></item>
    /// <item><description>Provides methods to send messages.</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Specifications:<br/>
    /// https://en.bitcoin.it/wiki/Protocol_documentation <br/>
    /// https://bitcoin.org/en/developer-reference#p2p-network
    /// </remarks>
    public class BitcoinEndpoint : IDisposable
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private const string UserAgent = "/BitcoinUtilities:0.0.1/";
        private static readonly int protocolVersion = 70002;
        private const ulong Services = 0;
        private const bool AcceptBroadcasts = false;
        private const int StartHeight = 0; // todo: support StartHeight
        private const ulong Nonce = 0; // todo: support Nonce

        private readonly BitcoinConnection connection;
        private readonly BitcoinPeerInfo peerInfo;
        private readonly CancellationTokenSource cancellationTokenSource;

        private Thread listenerThread;
        private BitcoinMessageHandler messageHandler;
        private Action<BitcoinEndpoint> disconnectHandler;

        private BitcoinEndpoint(BitcoinConnection connection, BitcoinPeerInfo peerInfo)
        {
            this.connection = connection;
            this.peerInfo = peerInfo;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Closes the underlying connection and signals the listener thread to stop.
        /// </summary>
        public void Dispose()
        {
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }

            connection.Dispose();
            cancellationTokenSource.Dispose();
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
        /// Creates an endpoint connected to a remote host.
        /// </summary>
        /// <param name="host">The DNS name of the remote host.</param>
        /// <param name="port">The port number of the remote host.</param>
        /// <param name="networkMagic">Four defined bytes which start every message in the Bitcoin P2P protocol to allow seeking to the next message.</param>
        /// <exception cref="BitcoinNetworkException">A network or protocol error occured during the handshake.</exception>
        public static BitcoinEndpoint Create(string host, int port, uint networkMagic)
        {
            return Create(BitcoinConnection.Connect(host, port, networkMagic));
        }

        /// <summary>
        /// <para>Creates an endpoint connected to a remote host using the provided connection.</para>
        /// <para>Performs a handshake with the remote host.</para>
        /// </summary>
        /// <param name="connection">The underlying connection.</param>
        /// <exception cref="BitcoinNetworkException">A network or protocol error occured during the handshake.</exception>
        public static BitcoinEndpoint Create(BitcoinConnection connection)
        {
            VersionMessage versionMessage;
            try
            {
                versionMessage = PerformHandshake(connection);
            }
            catch (BitcoinNetworkException)
            {
                connection.Dispose();
                throw;
            }

            return new BitcoinEndpoint(connection, new BitcoinPeerInfo(connection.RemoteEndPoint, versionMessage));
        }

        /// <summary>
        /// Performs a handshake with the remote host.
        /// </summary>
        /// <returns>A version message returned by the remote host.</returns>
        /// <exception cref="BitcoinNetworkException">A network or protocol error occured during the handshake.</exception>
        /// <remarks>
        /// Specification: https://en.bitcoin.it/wiki/Version_Handshake
        /// </remarks>
        private static VersionMessage PerformHandshake(BitcoinConnection connection)
        {
            VersionMessage outVersionMessage = CreateVersionMessage(connection);
            connection.WriteMessage(new BitcoinMessage(VersionMessage.Command, BitcoinStreamWriter.GetBytes(outVersionMessage.Write)));

            BitcoinMessage incVersionMessage = connection.ReadMessage();
            if (incVersionMessage.Command != VersionMessage.Command)
            {
                throw new BitcoinNetworkException("Remote endpoint did not send Version message.");
            }

            VersionMessage incVersionMessageParsed;
            try
            {
                incVersionMessageParsed = BitcoinStreamReader.FromBytes(incVersionMessage.Payload, VersionMessage.Read);
            }
            catch (EndOfStreamException e)
            {
                throw new BitcoinNetworkException("Received a malformed version message.", e);
            }

            //todo: check minimal peer protocol version

            connection.WriteMessage(new BitcoinMessage(VerAckMessage.Command, BitcoinStreamWriter.GetBytes(new VerAckMessage().Write)));

            BitcoinMessage incVerAckMessage = connection.ReadMessage();
            if (incVerAckMessage.Command != VerAckMessage.Command)
            {
                throw new BitcoinNetworkException("Remote endpoint did not send VerAck message.");
            }

            return incVersionMessageParsed;
        }

        /// <summary>
        /// Processes received messages.
        /// </summary>
        /// <param name="endpoint">An endpoint that received the message.</param>
        /// <param name="message">A message from the remote endpoint.</param>
        public delegate void BitcoinMessageHandler(BitcoinEndpoint endpoint, IBitcoinMessage message);

        /// <summary>
        /// Starts a thread that:
        /// <list type="bullet">
        /// <item><description>Handles incoming messages using provided handler method.</description></item>
        /// <item><description>Answers ping messages.</description></item>
        /// <item><description>Calls the given disconnect handler and disposes this endpoint when peer is disconnected.</description></item>
        /// </list>
        /// </summary>
        public void StartListener(BitcoinMessageHandler messageHandler, Action<BitcoinEndpoint> disconnectHandler)
        {
            if (listenerThread != null)
            {
                throw new InvalidOperationException("The endpoint already have an associated listener thread.");
            }

            listenerThread = new Thread(ListenerLoop);
            listenerThread.Name = "Endpoint Listener";
            listenerThread.IsBackground = true;

            this.messageHandler = messageHandler;
            this.disconnectHandler = disconnectHandler;

            listenerThread.Start();
        }

        private void ListenerLoop()
        {
            try
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    BitcoinMessage message;
                    try
                    {
                        message = connection.ReadMessage();
                    }
                    catch (BitcoinNetworkException)
                    {
                        // peer disconnected
                        break;
                    }

                    try
                    {
                        HandleMessage(message);
                    }
                    catch (BitcoinNetworkException)
                    {
                        // peer disconnected
                        break;
                    }
                    catch (Exception e)
                    {
                        logger.Error(e, "Unhandled exception in the message handler.");
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Unhandled exception in the endpoint listener thread.");
            }
            finally
            {
                try
                {
                    disconnectHandler(this);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Unhandled exception in the disconnect handler.");
                }
                finally
                {
                    Dispose();
                }
            }
        }

        /// <summary>
        /// Sends the given message to the connected peer. 
        /// </summary>
        /// <remarks>This method is thread-safe.</remarks>
        /// <param name="message">The message to send.</param>
        /// <exception cref="BitcoinNetworkException">A network failure occured.</exception>
        public void WriteMessage(IBitcoinMessage message)
        {
            BitcoinMessage rawMessage = new BitcoinMessage(message.Command, BitcoinStreamWriter.GetBytes(message.Write));
            connection.WriteMessage(rawMessage);
        }

        private void HandleMessage(BitcoinMessage message)
        {
            if (message.Command == RejectMessage.Command)
            {
                //todo: stop endpoint or allow message-handler to process it (be careful of reject message loop)?
                return;
            }

            IBitcoinMessage parsedMessage = BitcoinMessageParser.Parse(message);

            if (parsedMessage is PingMessage ping)
            {
                WriteMessage(new PongMessage(ping.Nonce));
            }

            if (parsedMessage == null)
            {
                // todo: consider creating new UknownMessage(message.Command, message.Payload) ?
                return;
            }

            messageHandler(this, parsedMessage);
        }

        private static VersionMessage CreateVersionMessage(BitcoinConnection conn)
        {
            long timestamp = UnixTime.ToTimestamp(SystemTime.UtcNow);
            IPEndPoint remoteEndPoint = conn.RemoteEndPoint;
            IPEndPoint localEndPoint = conn.LocalEndPoint; //todo: should it always be a public address ?

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
    }
}