using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BitcoinUtilities.P2P
{
    public class BitcoinConnectionListener : IDisposable
    {
        private readonly IPAddress address;
        private readonly int port;
        private readonly BitcoinConnectionHandler connectionHandler;

        private TcpListener tcpListener;
        private volatile bool running;

        public BitcoinConnectionListener(IPAddress address, int port, BitcoinConnectionHandler connectionHandler)
        {
            this.address = address;
            this.port = port;
            this.connectionHandler = connectionHandler;
        }

        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Starts listening for incoming connection requests.
        /// </summary>
        /// <exception cref="SocketException">If listener failed to use privided host or port to accept connections.</exception>
        public void Start()
        {
            tcpListener = new TcpListener(address, port);
            tcpListener.Start();

            running = true;

            Thread thread = new Thread(Listen);
            thread.Name = "Connection Listener";
            thread.IsBackground = true; //todo: should it be background?
            thread.Start();
        }

        private void Listen()
        {
            while (running)
            {
                TcpClient tcpClient;
                try
                {
                    tcpClient = tcpListener.AcceptTcpClient();
                }
                catch (SocketException)
                {
                    //todo: this happens when listener is stopped, can it happen for other reasons?
                    return;
                }
                BitcoinConnection conn = new BitcoinConnection(tcpClient);
                connectionHandler(conn);
            }
        }

        public void Stop()
        {
            running = false;
            if (tcpListener != null)
            {
                //todo: stop gracefully
                tcpListener.Stop();
                tcpListener = null;
            }
        }
    }

    public delegate void BitcoinConnectionHandler(BitcoinConnection connection);
}