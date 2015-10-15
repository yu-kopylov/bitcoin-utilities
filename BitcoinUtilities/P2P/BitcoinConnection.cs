﻿using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

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
        private const int MessageHeaderLength = 24;

        private const int MaxPayloadLength = 16*1024*1024;

        private readonly byte[] magicBytes = new byte[] {0xF9, 0xBE, 0xB4, 0xD9};

        private readonly SHA256 sha256ReaderAlg;
        private readonly SHA256 sha256WriterAlg;

        private TcpClient client;
        private NetworkStream stream;

        public BitcoinConnection()
        {
            sha256ReaderAlg = SHA256.Create();
            sha256WriterAlg = SHA256.Create();
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

        public void Connect(string host, int port)
        {
            client = new TcpClient(host, port);
            stream = client.GetStream();
        }

        public void WriteMessage(BitcoinMessage message)
        {
            byte[] header = new byte[24];
            Array.Copy(magicBytes, 0, header, 0, 4);
            Encoding.ASCII.GetBytes(message.Command, 0, message.Command.Length, header, 4);
            byte[] payloadLengthBytes = BitConverter.GetBytes(message.Payload.Length);
            Array.Copy(payloadLengthBytes, 0, header, 16, 4);

            byte[] checksum = sha256WriterAlg.ComputeHash(sha256WriterAlg.ComputeHash(message.Payload));
            Array.Copy(checksum, 0, header, 20, 4);

            stream.Write(header, 0, header.Length);
            stream.Write(message.Payload, 0, message.Payload.Length);
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

            return new BitcoinMessage(command, payload);
        }

        private byte[] ReadBytes(int count)
        {
            byte[] res = new byte[count];
            int bytesRead = 0;
            while (bytesRead < count)
            {
                bytesRead += stream.Read(res, bytesRead, count - bytesRead);
            }
            return res;
        }
    }
}