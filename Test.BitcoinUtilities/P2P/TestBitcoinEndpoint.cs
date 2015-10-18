using System;
using System.Collections.Generic;
using System.IO;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    public class TestBitcoinEndpoint
    {
        [Test]
        [Explicit]
        public void Test()
        {
            using (BitcoinEndpoint endpoint = new BitcoinEndpoint(LoggingMessageHandler))
            {
                endpoint.Connect("localhost", 8333);


                byte[] genesisBlockHash = new byte[]
                                          {
                                              0x00, 0x00, 0x00, 0x00, 0x00, 0x19, 0xd6, 0x68, 0x9c, 0x08, 0x5a, 0xe1, 0x65, 0x83, 0x1e, 0x93,
                                              0x4f, 0xf7, 0x63, 0xae, 0x46, 0xa2, 0xa6, 0xc1, 0x72, 0xb3, 0xf1, 0xb6, 0x0a, 0x8c, 0xe2, 0x6f
                                          };
                Array.Reverse(genesisBlockHash);

                List<byte[]> locatorHashes = new List<byte[]>();
                locatorHashes.Add(genesisBlockHash);
                GetBlocksMessage getBlocksMessage = new GetBlocksMessage(endpoint.ProtocolVersion, locatorHashes.ToArray(), genesisBlockHash);
                MemoryStream mem = new MemoryStream();
                using (BitcoinStreamWriter writer = new BitcoinStreamWriter(mem))
                {
                    getBlocksMessage.Write(writer);
                }
                BitcoinMessage msg = new BitcoinMessage(GetBlocksMessage.Command, mem.ToArray());
                endpoint.WriteMessage(msg);

                System.Threading.Thread.Sleep(30000);
            }
        }

        private bool LoggingMessageHandler(BitcoinEndpoint endpoint, BitcoinMessage message)
        {
            Console.WriteLine(">>command: {0}, payload: {1} bytes", message.Command, message.Payload.Length);

            if (message.Command == InvMessage.Command)
            {
                MemoryStream mem = new MemoryStream(message.Payload);
                using (BitcoinStreamReader reader = new BitcoinStreamReader(mem))
                {
                    //Console.WriteLine(">>payload: {0}", BitConverter.ToString(message.Payload));
                    InvMessage invMessage = InvMessage.Read(reader);
                    foreach (InventoryVector vector in invMessage.Inventory)
                    {
                        Console.WriteLine("\t{0} {1}", vector.Type.ToString().PadRight(16), BitConverter.ToString(vector.Hash));
                    }
                }
            }

            return true;
        }
    }
}