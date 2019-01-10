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
    /// A network connection with methods for reading and writing messages using Bitcoin P2P network protocol.
    /// </summary>
    /// <remarks>
    /// Specifications:<br/>
    /// https://en.bitcoin.it/wiki/Protocol_documentation <br/>
    /// https://bitcoin.org/en/developer-reference#p2p-network
    /// </remarks>
    public class BitcoinConnection : IDisposable
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private const int MessageHeaderLength = 24;
        private const int MaxCommandLength = 12;

        private const int MaxPayloadLength = 32 * 1024 * 1024;

        private readonly byte[] magicBytes = new byte[] {0xF9, 0xBE, 0xB4, 0xD9};

        private readonly object writeLock = new object();

        private readonly SHA256 sha256ReaderAlg;
        private readonly SHA256 sha256WriterAlg;

        private readonly TcpClient client;
        private readonly NetworkStream stream;

        private BitcoinConnection(TcpClient client, NetworkStream stream, SHA256 sha256ReaderAlg, SHA256 sha256WriterAlg)
        {
            this.client = client;
            this.stream = stream;
            this.sha256ReaderAlg = sha256ReaderAlg;
            this.sha256WriterAlg = sha256WriterAlg;
        }

        public void Dispose()
        {
            stream.Close();
            client.Close();

            sha256ReaderAlg.Dispose();
            sha256WriterAlg.Dispose();
        }

        /// <summary>
        /// Creates a connection to a remote host.
        /// </summary>
        /// <param name="client">The underlying connection.</param>
        /// <exception cref="BitcoinNetworkException">Connection failed.</exception>
        public static BitcoinConnection Connect(TcpClient client)
        {
            NetworkStream stream = null;
            SHA256 sha256ReaderAlg = null;
            SHA256 sha256WriterAlg = null;
            try
            {
                client.Client.ReceiveBufferSize = 32 * 1024 * 1024;
                client.Client.SendBufferSize = 32 * 1024 * 1024;
                stream = client.GetStream();

                sha256ReaderAlg = SHA256.Create();
                sha256WriterAlg = SHA256.Create();
            }
            catch (Exception e)
            {
                stream?.Close();
                client?.Close();
                sha256ReaderAlg?.Dispose();
                sha256WriterAlg?.Dispose();
                throw new BitcoinNetworkException("Connection failed.", e);
            }

            return new BitcoinConnection(client, stream, sha256ReaderAlg, sha256WriterAlg);
        }

        /// <summary>
        /// Connects to a remote host.
        /// </summary>
        /// <param name="host">The DNS name of the remote host.</param>
        /// <param name="port">The port number of the remote host.</param>
        /// <exception cref="BitcoinNetworkException">Connection failed.</exception>
        public static BitcoinConnection Connect(string host, int port)
        {
            TcpClient client;
            try
            {
                client = new TcpClient(host, port);
            }
            catch (Exception e)
            {
                throw new BitcoinNetworkException("Connection failed.", e);
            }

            return Connect(client);
        }

        public IPEndPoint LocalEndPoint
        {
            get { return client?.Client.LocalEndPoint as IPEndPoint; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return client?.Client.RemoteEndPoint as IPEndPoint; }
        }

        /// <summary>
        /// Sends the given message to the connected peer. 
        /// </summary>
        /// <remarks>This method is thread-safe.</remarks>
        /// <param name="message">The message to send.</param>
        /// <exception cref="ArgumentException">The given message is invalid.</exception>
        /// <exception cref="BitcoinNetworkException">A network failure occured.</exception>
        public void WriteMessage(BitcoinMessage message)
        {
            byte[] commandBytes = Encoding.ASCII.GetBytes(message.Command);
            if (commandBytes.Length > MaxCommandLength)
            {
                throw new ArgumentException(
                    $"Command length ({commandBytes.Length}) exeeds maximum command length ({MaxCommandLength}).", nameof(message)
                );
            }

            byte[] header = new byte[MessageHeaderLength];

            Array.Copy(magicBytes, 0, header, 0, 4);
            Array.Copy(commandBytes, 0, header, 4, commandBytes.Length);

            byte[] payloadLengthBytes = BitConverter.GetBytes(message.Payload.Length);
            Array.Copy(payloadLengthBytes, 0, header, 16, 4);

            lock (writeLock)
            {
                try
                {
                    byte[] checksum = sha256WriterAlg.ComputeHash(sha256WriterAlg.ComputeHash(message.Payload));
                    Array.Copy(checksum, 0, header, 20, 4);

                    stream.Write(header, 0, header.Length);
                    stream.Write(message.Payload, 0, message.Payload.Length);
                }
                catch (IOException e)
                {
                    throw new BitcoinNetworkException("Failed to send message.", e);
                }
                catch (ObjectDisposedException e)
                {
                    throw new BitcoinNetworkException("Failed to send message.", e);
                }
            }

            logger.Trace(() => $"Sent a message to the endpoint '{RemoteEndPoint}': {FormatForLog(message)}");
        }

        /// <summary>
        /// Reads a message from the network stream.
        /// </summary>
        /// <returns>The received message.</returns>
        /// <exception cref="BitcoinNetworkException">A network failure occured.</exception>
        public BitcoinMessage ReadMessage()
        {
            byte[] header = ReadBytes(MessageHeaderLength);

            int payloadLength = BitConverter.ToInt32(header, 16);

            for (int i = 0; i < 4; i++)
            {
                if (header[i] != magicBytes[i])
                {
                    throw new BitcoinNetworkException("The magic value is invalid.");
                }
            }

            if (payloadLength < 0)
            {
                throw new BitcoinNetworkException($"Invalid payload length: ({payloadLength}).");
            }

            if (payloadLength > MaxPayloadLength)
            {
                throw new BitcoinNetworkException($"Payload length is too large: {payloadLength}.");
            }

            int commandLength = 12;
            while (commandLength > 0 && header[commandLength + 4 - 1] == 0)
            {
                commandLength--;
            }

            string command = Encoding.ASCII.GetString(header, 4, commandLength);

            byte[] payload = ReadBytes(payloadLength);

            byte[] checksum;
            try
            {
                checksum = sha256ReaderAlg.ComputeHash(sha256ReaderAlg.ComputeHash(payload));
            }
            catch (ObjectDisposedException)
            {
                throw new BitcoinNetworkException("Connection is closed.");
            }

            for (int i = 0; i < 4; i++)
            {
                if (header[20 + i] != checksum[i])
                {
                    throw new BitcoinNetworkException("The checksum is invalid.");
                }
            }

            BitcoinMessage message = new BitcoinMessage(command, payload);

            logger.Trace(() => $"Received a message from the endpoint '{RemoteEndPoint}': {FormatForLog(message)}");

            return message;
        }

        /// <summary>
        /// Reads an array of bytes of the given length from the network stream.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <exception cref="BitcoinNetworkException">A network failure occured.</exception>
        private byte[] ReadBytes(int count)
        {
            byte[] res = new byte[count];
            int totalBytesRead = 0;
            while (totalBytesRead < count)
            {
                try
                {
                    int bytesRead = stream.Read(res, totalBytesRead, count - totalBytesRead);
                    if (bytesRead == 0)
                    {
                        throw new BitcoinNetworkException("Connection is closed.");
                    }

                    totalBytesRead += bytesRead;
                }
                catch (ObjectDisposedException e)
                {
                    throw new BitcoinNetworkException("Cannot read bytes from the network stream.", e);
                }
                catch (IOException e)
                {
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