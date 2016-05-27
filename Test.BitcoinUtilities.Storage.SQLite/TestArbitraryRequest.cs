using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BitcoinUtilities;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Storage.SQLite.Converters;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Storage.SQLite
{
    [TestFixture]
    public class TestArbitraryRequest
    {
        private static readonly Dictionary<int, byte[]> knownBlockHashesByHeight = new Dictionary<int, byte[]>
                                                                                   {
                                                                                       {
                                                                                           6,
                                                                                           new byte[]
                                                                                           {
                                                                                               0x8D, 0x77, 0x8F, 0xDC, 0x15, 0xA2, 0xD3, 0xFB, 0x76, 0xB7, 0x12, 0x2A, 0x3B, 0x55, 0x82, 0xBE,
                                                                                               0xA4, 0xF2, 0x1F, 0x5A, 0x0C, 0x69, 0x35, 0x37, 0xE7, 0xA0, 0x31, 0x30, 0x00, 0x00, 0x00, 0x00
                                                                                           }
                                                                                       },
                                                                                       {
                                                                                           7,
                                                                                           new byte[]
                                                                                           {
                                                                                               0x44, 0x94, 0xC8, 0xCF, 0x41, 0x54, 0xBD, 0xCC, 0x07, 0x20, 0xCD, 0x4A, 0x59, 0xD9, 0xC9, 0xB2,
                                                                                               0x85, 0xE4, 0xB1, 0x46, 0xD4, 0x5F, 0x06, 0x1D, 0x2B, 0x6C, 0x96, 0x71, 0x00, 0x00, 0x00, 0x00
                                                                                           }
                                                                                       },
                                                                                       {
                                                                                           8,
                                                                                           new byte[]
                                                                                           {
                                                                                               0xC6, 0x0D, 0xDE, 0xF1, 0xB7, 0x61, 0x8C, 0xA2, 0x34, 0x8A, 0x46, 0xE8, 0x68, 0xAF, 0xC2, 0x6E,
                                                                                               0x3E, 0xFC, 0x68, 0x22, 0x6C, 0x78, 0xAA, 0x47, 0xF8, 0x48, 0x8C, 0x40, 0x00, 0x00, 0x00, 0x00
                                                                                           }
                                                                                       },
                                                                                       {
                                                                                           19,
                                                                                           new byte[]
                                                                                           {
                                                                                               0x6F, 0x18, 0x7F, 0xDD, 0xD5, 0xE2, 0x8A, 0xA1, 0xB4, 0x06, 0x5D, 0xAA, 0x5D, 0x9E, 0xAE, 0x0C,
                                                                                               0x48, 0x70, 0x94, 0xFB, 0x20, 0xCF, 0x97, 0xCA, 0x02, 0xB8, 0x1C, 0x84, 0x00, 0x00, 0x00, 0x00
                                                                                           }
                                                                                       },
                                                                                       {
                                                                                           20,
                                                                                           new byte[]
                                                                                           {
                                                                                               0xD7, 0xC8, 0x34, 0xE8, 0xEA, 0x05, 0xE2, 0xC2, 0xFD, 0xDF, 0x4D, 0x82, 0xFA, 0xF4, 0xC3, 0xE9,
                                                                                               0x21, 0x02, 0x7F, 0xA1, 0x90, 0xF1, 0xB8, 0x37, 0x2A, 0x7A, 0xA9, 0x67, 0x00, 0x00, 0x00, 0x00
                                                                                           }
                                                                                       },
                                                                                       {
                                                                                           403,
                                                                                           new byte[]
                                                                                           {
                                                                                               0x5A, 0x73, 0x0B, 0x9E, 0xE0, 0x64, 0x2B, 0xDC, 0xF9, 0x38, 0x3E, 0xF5, 0xA7, 0x2B, 0xF9, 0x84,
                                                                                               0x58, 0x3A, 0xC6, 0xA8, 0x93, 0xAD, 0x7D, 0xA8, 0xB7, 0x53, 0xE9, 0x70, 0x00, 0x00, 0x00, 0x00
                                                                                           }
                                                                                       },
                                                                                       {
                                                                                           999,
                                                                                           new byte[]
                                                                                           {
                                                                                               0x00, 0x00, 0x00, 0x00, 0x08, 0xe6, 0x47, 0x74, 0x27, 0x75, 0xa2, 0x30, 0x78, 0x7d, 0x66, 0xfd,
                                                                                               0xf9, 0x2c, 0x46, 0xa4, 0x8c, 0x89, 0x6b, 0xfb, 0xc8, 0x5c, 0xdc, 0x8a, 0xcc, 0x67, 0xe8, 0x7d
                                                                                           }
                                                                                       },
                                                                                       {
                                                                                           1000,
                                                                                           new byte[]
                                                                                           {
                                                                                               0x00, 0x00, 0x00, 0x00, 0xc9, 0x37, 0x98, 0x37, 0x04, 0xa7, 0x3a, 0xf2, 0x8a, 0xcd, 0xec, 0x37,
                                                                                               0xb0, 0x49, 0xd2, 0x14, 0xad, 0xbd, 0xa8, 0x1d, 0x7e, 0x2a, 0x3d, 0xd1, 0x46, 0xf6, 0xed, 0x09
                                                                                           }
                                                                                       },
                                                                                       {
                                                                                           9999,
                                                                                           new byte[]
                                                                                           {
                                                                                               0x00, 0x00, 0x00, 0x00, 0xfb, 0xc9, 0x7c, 0xc6, 0xc5, 0x99, 0xce, 0x9c, 0x24, 0xdd, 0x4a, 0x22,
                                                                                               0x43, 0xe2, 0xbf, 0xd5, 0x18, 0xed, 0xa5, 0x6e, 0x1d, 0x5e, 0x47, 0xd2, 0x9e, 0x29, 0xc3, 0xa7
                                                                                           }
                                                                                       },
                                                                                       {
                                                                                           10000,
                                                                                           new byte[]
                                                                                           {
                                                                                               0x00, 0x00, 0x00, 0x00, 0x99, 0xc7, 0x44, 0x45, 0x5f, 0x58, 0xe6, 0xc6, 0xe9, 0x8b, 0x67, 0x1e,
                                                                                               0x1b, 0xf7, 0xf3, 0x73, 0x46, 0xbf, 0xd4, 0xcf, 0x5d, 0x02, 0x74, 0xad, 0x8e, 0xe6, 0x60, 0xcb
                                                                                           }
                                                                                       },
                                                                                       {
                                                                                           127444,
                                                                                           new byte[]
                                                                                           {
                                                                                               0xF6, 0xE7, 0xB2, 0x85, 0xE2, 0x95, 0x67, 0x0D, 0xB5, 0x74, 0xD8, 0xB7, 0x5F, 0x6D, 0x8A, 0x0A,
                                                                                               0x6D, 0x6C, 0x06, 0x54, 0xFD, 0x88, 0x35, 0x22, 0x6A, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                                                                                           }
                                                                                       },
                                                                                       {
                                                                                           127445,
                                                                                           new byte[]
                                                                                           {
                                                                                               0x85, 0x43, 0x10, 0x2F, 0x51, 0xB5, 0x7D, 0x12, 0x94, 0xA2, 0xC8, 0xE6, 0xB0, 0x30, 0x39, 0x39,
                                                                                               0xCE, 0x1F, 0xC1, 0xCB, 0xD5, 0xDA, 0xFD, 0x07, 0x69, 0x26, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                                                                                           }
                                                                                       }
                                                                                   };

        private static readonly Dictionary<byte[], int> knownBlockHashesByHash = new Dictionary<byte[], int>(ByteArrayComparer.Instance);

        static TestArbitraryRequest()
        {
            Array.Reverse(knownBlockHashesByHeight[999]);
            Array.Reverse(knownBlockHashesByHeight[1000]);
            Array.Reverse(knownBlockHashesByHeight[9999]);
            Array.Reverse(knownBlockHashesByHeight[10000]);

            foreach (KeyValuePair<int, byte[]> pair in knownBlockHashesByHeight)
            {
                knownBlockHashesByHash.Add(pair.Value, pair.Key);
            }
        }

        [Test]
        [Explicit]
        public void Test()
        {
            using (BitcoinEndpoint ep = new BitcoinEndpoint(LogMessage))
            {
                ep.Connect("127.0.0.1", 8333);
//                SendGetBlocks(ep, knownBlockHashes[6]);
                SendGetBlocks(ep, knownBlockHashesByHeight[127445]);
//                SendGetBlocks(ep, knownBlockHashesByHeight[9999]);

                Thread.Sleep(1000);

                //SendGetBlocks(ep, knownBlockHashesByHeight[1000]);
                SendGetData(ep, new InventoryVector(InventoryVectorType.MsgBlock, knownBlockHashesByHeight[127445]));
                SendGetData(ep, new InventoryVector(InventoryVectorType.MsgBlock, new byte[]
                                                                                  {
                                                                                      0x35, 0xA5, 0x81, 0x56, 0x24, 0x6B, 0x1D, 0xF0, 0xAB, 0x42, 0xC1, 0x14, 0xAE, 0x46, 0xAB, 0x7E,
                                                                                      0x09, 0x5A, 0x80, 0x63, 0xDB, 0x5E, 0x3A, 0x98, 0x4D, 0x1B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                                                                                  }));

//                foreach (KeyValuePair<int, byte[]> pair in knownBlockHashesByHeight)
//                {
//                    SendGetData(ep, InventoryVectorType.MsgBlock, new InventoryVector[]
//                                                                  {
//                                                                      new InventoryVector(InventoryVectorType.MsgBlock, pair.Value),
//                                                                  });
//
//                }
//                SendGetData(ep, InventoryVectorType.MsgBlock, new InventoryVector[]
//                                                              {
//                                                                  new InventoryVector(InventoryVectorType.MsgBlock, knownBlockHashesByHeight[7]),
//                                                                  new InventoryVector(InventoryVectorType.MsgBlock, knownBlockHashesByHeight[8]),
//                                                                  new InventoryVector(InventoryVectorType.MsgBlock, knownBlockHashesByHeight[20]),
//                                                                  new InventoryVector(InventoryVectorType.MsgBlock, knownBlockHashesByHeight[403]),
//                                                                  new InventoryVector(InventoryVectorType.MsgBlock, knownBlockHashesByHeight[1000]),
//                                                                  new InventoryVector(InventoryVectorType.MsgBlock, knownBlockHashesByHeight[10000]),
//                                                              });
//
//                SendGetData(ep, InventoryVectorType.MsgBlock, new InventoryVector[]
//                                                              {
//                                                                  new InventoryVector(InventoryVectorType.MsgBlock, knownBlockHashesByHeight[1000]),
//                                                              });

                Thread.Sleep(1000);
            }
        }

        private void SendGetBlocks(BitcoinEndpoint ep, byte[] lastHash)
        {
            GetBlocksMessage message = new GetBlocksMessage(ep.ProtocolVersion, new byte[][] {lastHash}, new byte[32]);
            ep.WriteMessage(message);
            Console.WriteLine("> => {0} [{1}]", GetBlocksMessage.Command, BitConverter.ToString(lastHash));
        }

        private void SendGetData(BitcoinEndpoint ep, params InventoryVector[] vectors)
        {
            GetDataMessage message = new GetDataMessage(vectors);
            ep.WriteMessage(message);
            Console.WriteLine("> => {0} [{1}]", GetDataMessage.Command, message.Inventory.Count);
        }

        private bool LogMessage(BitcoinEndpoint endpoint, IBitcoinMessage message)
        {
            InvMessage invMessage = message as InvMessage;
            BlockMessage blockMessage = message as BlockMessage;

            if (invMessage != null)
            {
                Console.WriteLine("> <= {0} [{1}] {{{2} .. {3}}}", message.Command, invMessage.Inventory.Count, BitConverter.ToString(invMessage.Inventory.First().Hash),
                    BitConverter.ToString(invMessage.Inventory.Last().Hash));
            }
            else if (blockMessage != null)
            {
                BlockConverter converter = new BlockConverter();
                byte[] hash = converter.FromMessage(blockMessage).Hash;
                int height;
                if (!knownBlockHashesByHash.TryGetValue(hash, out height))
                {
                    height = -1;
                }
                Console.WriteLine("> <= {0} ({1}) {2}", message.Command, height < 0 ? "????" : height.ToString(), BitConverter.ToString(hash));
            }
            else
            {
                Console.WriteLine("> <= {0}", message.Command);
            }
            return true;
        }
    }
}