using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using NLog;

namespace BitcoinUtilities.P2P
{
    /// <summary>
    /// Provides a P2P Bitcoin network connection with methods for a message exchange.
    /// </summary>
    /// <remarks>
    /// Specifications:<br/>
    /// https://en.bitcoin.it/wiki/Protocol_documentation <br/>
    /// https://bitcoin.org/en/developer-reference#p2p-network
    /// </remarks>
    public class BitcoinConnection : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const int MessageHeaderLength = 24;
        private const int MaxCommandLength = 12;

        private const int MaxPayloadLength = 32*1024*1024;

        private readonly byte[] magicBytes = new byte[] {0xF9, 0xBE, 0xB4, 0xD9};

        private readonly object writeLock = new object();

        private readonly SHA256 sha256ReaderAlg;
        private readonly SHA256 sha256WriterAlg;

        private TcpClient client;
        private NetworkStream stream;

        public BitcoinConnection()
        {
            sha256ReaderAlg = SHA256.Create();
            sha256WriterAlg = SHA256.Create();
        }

        public BitcoinConnection(TcpClient client) : this()
        {
            this.client = client;
            this.stream = client.GetStream();
        }

        public void Dispose()
        {
            sha256ReaderAlg.Dispose();
            sha256WriterAlg.Dispose();

            if (stream != null)
            {
                stream.Close();
            }
            if (client != null)
            {
                client.Close();
            }
        }

        /// <summary>
        /// Connects to a remote host.
        /// </summary>
        /// <param name="host">The DNS name of the remote host.</param>
        /// <param name="port">The port number of the remote host.</param>
        /// <exception cref="BitcoinNetworkException">Connection failed.</exception>
        public void Connect(string host, int port)
        {
            if (client != null)
            {
                throw new BitcoinNetworkException("A connection was already established.");
            }

            try
            {
                client = new TcpClient(host, port);
            }
            catch (SocketException e)
            {
                throw new BitcoinNetworkException("Connection failed.", e);
            }

            //todo: also implement this in BitcoinConnectionListener
            client.Client.ReceiveBufferSize = 32*1024*1024;
            client.Client.SendBufferSize = 32*1024*1024;

            stream = client.GetStream();
        }

        public IPEndPoint LocalEndPoint
        {
            get
            {
                if (client == null)
                {
                    return null;
                }
                return client.Client.LocalEndPoint as IPEndPoint;
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                if (client == null)
                {
                    return null;
                }
                return client.Client.RemoteEndPoint as IPEndPoint;
            }
        }

        /// <summary>
        /// Sends the message to the connected peer.
        /// <para/>
        /// Method is thread-safe.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void WriteMessage(BitcoinMessage message)
        {
            lock (writeLock)
            {
                byte[] commandBytes = Encoding.ASCII.GetBytes(message.Command);
                if (commandBytes.Length > MaxCommandLength)
                {
                    //todo: handle correctly
                    throw new BitcoinNetworkException($"Command length ({commandBytes.Length}) exeeds maximum command length ({MaxCommandLength}).");
                }

                byte[] header = new byte[MessageHeaderLength];

                Array.Copy(magicBytes, 0, header, 0, 4);
                Array.Copy(commandBytes, 0, header, 4, commandBytes.Length);

                byte[] payloadLengthBytes = BitConverter.GetBytes(message.Payload.Length);
                Array.Copy(payloadLengthBytes, 0, header, 16, 4);

                byte[] checksum = sha256WriterAlg.ComputeHash(sha256WriterAlg.ComputeHash(message.Payload));
                Array.Copy(checksum, 0, header, 20, 4);

                stream.Write(header, 0, header.Length);
                stream.Write(message.Payload, 0, message.Payload.Length);
            }

            if (logger.IsTraceEnabled)
            {
                logger.Trace("Sent message: {0}", FormatForLog(message));
            }
        }

        public BitcoinMessage ReadMessage()
        {
            byte[] header = ReadBytes(MessageHeaderLength);

            int payloadLength = BitConverter.ToInt32(header, 16);

            for (int i = 0; i < 4; i++)
            {
                if (header[i] != magicBytes[i])
                {
                    //todo: handle correctly
                    throw new Exception("The magic value is invalid.");
                }
            }

            if (payloadLength < 0)
            {
                //todo: handle correctly
                throw new Exception(string.Format("Invalid payload length: ({0}).", payloadLength));
            }

            //todo: move to const
            if (payloadLength > MaxPayloadLength)
            {
                //todo: handle correctly
                throw new Exception(string.Format("Payload length is too large: {0}.", payloadLength));
            }

            int commandLength = 12;
            while (commandLength > 0 && header[commandLength + 4 - 1] == 0)
            {
                commandLength--;
            }

            string command = Encoding.ASCII.GetString(header, 4, commandLength);

            byte[] payload = ReadBytes(payloadLength);

            byte[] checksum = sha256ReaderAlg.ComputeHash(sha256ReaderAlg.ComputeHash(payload));

            for (int i = 0; i < 4; i++)
            {
                if (header[20 + i] != checksum[i])
                {
                    //todo: handle correctly
                    throw new Exception("The checksum is invalid.");
                }
            }

            BitcoinMessage message = new BitcoinMessage(command, payload);

            if (logger.IsTraceEnabled)
            {
                logger.Trace("Received message: {0}", FormatForLog(message));
            }

            return message;
        }

        private byte[] ReadBytes(int count)
        {
            byte[] res = new byte[count];
            int bytesRead = 0;
            while (bytesRead < count)
            {
                try
                {
                    bytesRead += stream.Read(res, bytesRead, count - bytesRead);
                }
                catch (IOException e)
                {
                    //todo: test if write methods also require exception wrapping
                    throw new BitcoinNetworkException("Cannot read bytes from the network stream.", e);
                }
            }
            return res;
        }

        private string FormatForLog(BitcoinMessage message)
        {
            StringBuilder sb = new StringBuilder();
            BitcoinMessageFormatter formatter = new BitcoinMessageFormatter("\t");
            sb.AppendFormat("{0} [{1} byte(s)]", message.Command, message.Payload.Length);
            try
            {
                IBitcoinMessage parsedMessage = BitcoinMessageParser.Parse(message);
                if (parsedMessage != null)
                {
                    sb.Append("\n");
                    sb.Append(formatter.Format(parsedMessage));
                }
            }
            catch (Exception e)
            {
                sb.AppendFormat("\n\tUnable to parse message: {0}", e.Message);
            }
            return sb.ToString();
        }
    }
}