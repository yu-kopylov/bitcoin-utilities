using System;
using System.Collections;
using System.Net;
using System.Text;
using System.Threading;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    public class TestBitcoinEndpoint
    {
        private static readonly byte[] blockHash0 = new byte[]
        {
            0x6F, 0xE2, 0x8C, 0x0A, 0xB6, 0xF1, 0xB3, 0x72, 0xC1, 0xA6, 0xA2, 0x46, 0xAE, 0x63, 0xF7, 0x4F,
            0x93, 0x1E, 0x83, 0x65, 0xE1, 0x5A, 0x08, 0x9C, 0x68, 0xD6, 0x19, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private static readonly byte[] blockHash1 = new byte[]
        {
            0x48, 0x60, 0xEB, 0x18, 0xBF, 0x1B, 0x16, 0x20, 0xE3, 0x7E, 0x94, 0x90, 0xFC, 0x8A, 0x42, 0x75,
            0x14, 0x41, 0x6F, 0xD7, 0x51, 0x59, 0xAB, 0x86, 0x68, 0x8E, 0x9A, 0x83, 0x00, 0x00, 0x00, 0x00
        };

        private static readonly byte[] blockHash2 = new byte[]
        {
            0xBD, 0xDD, 0x99, 0xCC, 0xFD, 0xA3, 0x9D, 0xA1, 0xB1, 0x08, 0xCE, 0x1A, 0x5D, 0x70, 0x03, 0x8D,
            0x0A, 0x96, 0x7B, 0xAC, 0xB6, 0x8B, 0x6B, 0x63, 0x06, 0x5F, 0x62, 0x6A, 0x00, 0x00, 0x00, 0x00
        };


        [Test]
        [Explicit]
        public void TestServer()
        {
            using (BitcoinConnectionListener listener = BitcoinConnectionListener.StartListener(IPAddress.Any, 8334, ConnectionHandler))
            {
                Thread.Sleep(30000);
            }
        }

        private void ConnectionHandler(BitcoinConnection connection)
        {
            using (BitcoinEndpoint endpoint = new BitcoinEndpoint(LoggingMessageHandler))
            {
                endpoint.Connect(connection);

                Thread.Sleep(1000);
                endpoint.WriteMessage(new BitcoinMessage(InvMessage.Command, new byte[0]));
            }
        }

        [Test]
        [Explicit]
        public void TestClient()
        {
            //todo: test ping deadlock

            using (BitcoinEndpoint endpoint = new BitcoinEndpoint(LoggingMessageHandler))
            {
                endpoint.Connect("localhost", 8333);

                //endpoint.WriteMessage(new GetBlocksMessage(endpoint.ProtocolVersion, new byte[][] {blockHash0}, blockHash2));
                endpoint.WriteMessage(new GetBlocksMessage(endpoint.ProtocolVersion, new byte[][] {new byte[32]}, new byte[32]));
                Thread.Sleep(500);
                endpoint.WriteMessage(new GetDataMessage(new InventoryVector[] {new InventoryVector(InventoryVectorType.MsgBlock, blockHash1)}));

                Thread.Sleep(30000);
            }
        }

        private bool LoggingMessageHandler(BitcoinEndpoint endpoint, IBitcoinMessage message)
        {
            Console.WriteLine(">>command: {0}", message.Command);

            if (message is InvMessage)
            {
                InvMessage invMessage = (InvMessage) message;
                foreach (InventoryVector vector in invMessage.Inventory)
                {
                    Console.WriteLine("\t{0} {1}", vector.Type.ToString().PadRight(16), BitConverter.ToString(vector.Hash));
                }
            }

            if (message is GetHeadersMessage)
            {
                GetHeadersMessage getHeadersMessage = (GetHeadersMessage) message;

                Console.WriteLine("\tprotocol: {0}", getHeadersMessage.ProtocolVersion);
                foreach (byte[] hash in getHeadersMessage.LocatorHashes)
                {
                    Console.WriteLine("\t{0}", BitConverter.ToString(hash));
                }

                Console.WriteLine("\t{0} (stop)", BitConverter.ToString(getHeadersMessage.HashStop));

                endpoint.WriteMessage(new InvMessage(new InventoryVector[]
                {
                    new InventoryVector(InventoryVectorType.MsgBlock, blockHash1)
                }));
            }

            if (message is GetDataMessage)
            {
                GetDataMessage getDataMessage = (GetDataMessage) message;
                foreach (InventoryVector vector in getDataMessage.Inventory)
                {
                    Console.WriteLine("\t{0} {1}", vector.Type.ToString().PadRight(16), BitConverter.ToString(vector.Hash));
                }
            }

            if (message is BlockMessage)
            {
                BlockMessage blockMessage = (BlockMessage) message;
                foreach (Tx tx in blockMessage.Transactions)
                {
                    Console.WriteLine("\tInputs:");
                    foreach (TxIn input in tx.Inputs)
                    {
                        Console.WriteLine("\t\t{0} seq. {1:X}", BitConverter.ToString(input.PreviousOutput.Hash), input.Sequence);
                    }

                    Console.WriteLine("\tOutputs:");
                    foreach (TxOut output in tx.Outputs)
                    {
                        Console.WriteLine("\t\t{0}, script: {1}", output.Value, BitConverter.ToString(output.PubkeyScript));
                    }
                }
            }

            if (message is MerkleBlockMessage)
            {
                MerkleBlockMessage merkleBlockMessage = (MerkleBlockMessage) message;

                BitArray flagsBitArray = new BitArray(merkleBlockMessage.Flags);
                StringBuilder flagsSb = new StringBuilder();
                foreach (bool flag in flagsBitArray)
                {
                    flagsSb.Append(flag ? "1" : "0");
                }

                Console.WriteLine("\tTotalTransactions: {0}", merkleBlockMessage.TotalTransactions);
                Console.WriteLine("\tFlags:             {0}", flagsSb);

                foreach (byte[] hash in merkleBlockMessage.Hashes)
                {
                    Console.WriteLine("\t\t{0}", BitConverter.ToString(hash));
                }
            }

            if (message is BitcoinMessage)
            {
                BitcoinMessage rawMessage = (BitcoinMessage) message;
                Console.WriteLine(">>payload: {0}", BitConverter.ToString(rawMessage.Payload));
            }

            return true;
        }

        [Test]
        public void TestDeadlock()
        {
            AutoResetEvent finishedEvent = new AutoResetEvent(false);
            using (BitcoinConnectionListener server = BitcoinConnectionListener.StartListener(IPAddress.Loopback, 28333, DeadlockTestConnectionHandler))
            {
                Exception threadException = null;

                Thread thread = new Thread(() =>
                {
                    try
                    {
                        using (BitcoinEndpoint client = new BitcoinEndpoint(DeadlockTestMessageHandler))
                        {
                            client.Connect("localhost", 28333);
                            SendDeadlockTestSequence(client, "client");
                            //let other peer to send remaining data
                            Thread.Sleep(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        threadException = ex;
                    }
                    finally
                    {
                        finishedEvent.Set();
                    }
                });
                thread.Name = "Test Sending Thread";
                thread.IsBackground = true;
                thread.Start();
                Assert.That(finishedEvent.WaitOne(5000), Is.True);
                Assert.That(threadException, Is.Null);
            }
        }

        private void SendDeadlockTestSequence(BitcoinEndpoint client, string peerName)
        {
            byte[][] locatorHashes = new byte[256][];
            for (int i = 0; i < locatorHashes.Length; i++)
            {
                locatorHashes[i] = new byte[32];
            }

            IBitcoinMessage pingMessage = new PingMessage(0);

            IBitcoinMessage bigMessage = new GetBlocksMessage(client.ProtocolVersion, locatorHashes, new byte[32]);
            byte[] messageText = BitcoinStreamWriter.GetBytes(bigMessage.Write);
            Console.WriteLine("{0}: message length is {1}", peerName, messageText.Length);

            Console.WriteLine("{0}: sequence started", peerName);
            for (int i = 0; i < 16; i++)
            {
                client.WriteMessage(bigMessage);
                client.WriteMessage(pingMessage);
            }

            Console.WriteLine("{0}: sequence finished", peerName);
        }

        private void DeadlockTestConnectionHandler(BitcoinConnection connection)
        {
            using (BitcoinEndpoint endpoint = new BitcoinEndpoint(DeadlockTestMessageHandler))
            {
                endpoint.Connect(connection);

                SendDeadlockTestSequence(endpoint, "server");

                //let other peer to send remaining data
                Thread.Sleep(1000);
            }
        }

        private bool DeadlockTestMessageHandler(BitcoinEndpoint endpoint, IBitcoinMessage message)
        {
            Thread.Sleep(100);
            return true;
        }
    }
}