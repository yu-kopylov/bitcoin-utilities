using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NLog;

namespace BitcoinUtilities.P2P
{
    /// <summary>
    /// A thread that listens for incoming connections.
    /// </summary>
    public class BitcoinConnectionListener : IDisposable
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly TcpListener tcpListener;
        private readonly Thread listenerThread;
        private readonly CancellationTokenSource cancellationTokenSource;

        private BitcoinConnectionListener(TcpListener tcpListener, Thread listenerThread, CancellationTokenSource cancellationTokenSource)
        {
            this.tcpListener = tcpListener;
            this.cancellationTokenSource = cancellationTokenSource;
            this.listenerThread = listenerThread;
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
            tcpListener.Stop();
            listenerThread.Join();
        }

        /// <summary>
        /// The port on which this listener listens for incoming connections.
        /// </summary>
        public int Port => ((IPEndPoint) tcpListener.LocalEndpoint).Port;

        public delegate void BitcoinConnectionHandler(BitcoinConnection connection);

        /// <summary>
        /// Starts a new thread to listen for incoming connections.
        /// </summary>
        /// <param name="address">The address on which to listen for incoming connections.</param>
        /// <param name="port">The port on which to listen for incoming connections.</param>
        /// <param name="connectionHandler">An action to perform when connection is accepted.</param>
        /// <returns>A new instance of <see cref="BitcoinConnectionListener"/>.</returns>
        /// <exception cref="SocketException">If listener failed to use privided host and port to accept connections.</exception>
        public static BitcoinConnectionListener StartListener(IPAddress address, int port, BitcoinConnectionHandler connectionHandler)
        {
            TcpListener tcpListener = new TcpListener(address, port);
            tcpListener.Start();

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            ListenerThreadParams threadParams = new ListenerThreadParams(tcpListener, cancellationTokenSource.Token, connectionHandler);

            Thread listenerThread;
            try
            {
                listenerThread = new Thread(ListenerLoop);
                listenerThread.Name = "Connection Listener";
                listenerThread.IsBackground = true;
                listenerThread.Start(threadParams);
            }
            catch (Exception)
            {
                tcpListener.Stop();
                throw;
            }

            return new BitcoinConnectionListener(tcpListener, listenerThread, cancellationTokenSource);
        }

        private sealed class ListenerThreadParams
        {
            public ListenerThreadParams(TcpListener tcpListener, CancellationToken cancellationToken, BitcoinConnectionHandler connectionHandler)
            {
                TcpListener = tcpListener;
                CancellationToken = cancellationToken;
                ConnectionHandler = connectionHandler;
            }

            public TcpListener TcpListener { get; }
            public CancellationToken CancellationToken { get; }
            public BitcoinConnectionHandler ConnectionHandler { get; }
        }

        private static void ListenerLoop(object parameters)
        {
            try
            {
                ListenerThreadParams threadParams = (ListenerThreadParams) parameters;
                while (!threadParams.CancellationToken.IsCancellationRequested)
                {
                    TcpClient tcpClient;
                    try
                    {
                        tcpClient = threadParams.TcpListener.AcceptTcpClient();
                    }
                    catch (SocketException)
                    {
                        // listener is stopped
                        return;
                    }

                    try
                    {
                        BitcoinConnection conn = BitcoinConnection.Connect(tcpClient);
                        threadParams.ConnectionHandler(conn);
                    }
                    catch (BitcoinNetworkException)
                    {
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Unhandled exception in the connection listener thread.");
            }
        }
    }
}