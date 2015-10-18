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

        public void Start()
        {
            tcpListener = new TcpListener(address, port);
            tcpListener.Start();

            running = true;

            Thread thread = new Thread(Listen);
            thread.IsBackground = true; //todo: should it be background?
            thread.Start();
        }

        private void Listen()
        {
            while (running)
            {
                TcpClient tcpClient = tcpListener.AcceptTcpClient();
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