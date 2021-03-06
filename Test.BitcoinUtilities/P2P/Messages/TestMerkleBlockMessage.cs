﻿using System.IO;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P.Messages
{
    public class TestMerkleBlockMessage
    {
        [Test]
        public void Test()
        {
            byte[] inBytes = new byte[]
                             {
                                 0x01, 0x00, 0x00, 0x00,
                                 0x82, 0xbb, 0x86, 0x9c, 0xf3, 0xa7, 0x93, 0x43, 0x2a, 0x66, 0xe8, 0x26, 0xe0, 0x5a, 0x6f, 0xc3,
                                 0x74, 0x69, 0xf8, 0xef, 0xb7, 0x42, 0x1d, 0xc8, 0x80, 0x67, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
                                 0x7f, 0x16, 0xc5, 0x96, 0x2e, 0x8b, 0xd9, 0x63, 0x65, 0x9c, 0x79, 0x3c, 0xe3, 0x70, 0xd9, 0x5f,
                                 0x09, 0x3b, 0xc7, 0xe3, 0x67, 0x11, 0x7b, 0x3c, 0x30, 0xc1, 0xf8, 0xfd, 0xd0, 0xd9, 0x72, 0x87,
                                 0x76, 0x38, 0x1b, 0x4d,
                                 0x4c, 0x86, 0x04, 0x1b,
                                 0x55, 0x4b, 0x85, 0x29,
                                 0x07, 0x00, 0x00, 0x00,
                                 0x04,
                                 0x36, 0x12, 0x26, 0x26, 0x24, 0x04, 0x7e, 0xe8, 0x76, 0x60, 0xbe, 0x1a, 0x70, 0x75, 0x19, 0xa4,
                                 0x43, 0xb1, 0xc1, 0xce, 0x3d, 0x24, 0x8c, 0xbf, 0xc6, 0xc1, 0x58, 0x70, 0xf6, 0xc5, 0xda, 0xa2,
                                 0x01, 0x9f, 0x5b, 0x01, 0xd4, 0x19, 0x5e, 0xcb, 0xc9, 0x39, 0x8f, 0xbf, 0x3c, 0x3b, 0x1f, 0xa9,
                                 0xbb, 0x31, 0x83, 0x30, 0x1d, 0x7a, 0x1f, 0xb3, 0xbd, 0x17, 0x4f, 0xcf, 0xa4, 0x0a, 0x2b, 0x65,
                                 0x41, 0xed, 0x70, 0x55, 0x1d, 0xd7, 0xe8, 0x41, 0x88, 0x3a, 0xb8, 0xf0, 0xb1, 0x6b, 0xf0, 0x41,
                                 0x76, 0xb7, 0xd1, 0x48, 0x0e, 0x4f, 0x0a, 0xf9, 0xf3, 0xd4, 0xc3, 0x59, 0x57, 0x68, 0xd0, 0x68,
                                 0x20, 0xd2, 0xa7, 0xbc, 0x99, 0x49, 0x87, 0x30, 0x2e, 0x5b, 0x1a, 0xc8, 0x0f, 0xc4, 0x25, 0xfe,
                                 0x25, 0xf8, 0xb6, 0x31, 0x69, 0xea, 0x78, 0xe6, 0x8f, 0xba, 0xae, 0xfa, 0x59, 0x37, 0x9b, 0xbf,
                                 0x01,
                                 0x1d
                             };

            MerkleBlockMessage message;

            MemoryStream inStream = new MemoryStream(inBytes);
            using (BitcoinStreamReader reader = new BitcoinStreamReader(inStream))
            {
                message = MerkleBlockMessage.Read(reader);
            }

            Assert.That(message.BlockHeader.Version, Is.EqualTo(1));
            Assert.That(message.BlockHeader.Timestamp, Is.EqualTo(1293629558));
            Assert.That(message.BlockHeader.NBits, Is.EqualTo(0x1B04864C));
            Assert.That(message.BlockHeader.Nonce, Is.EqualTo(0x29854B55));
            Assert.That(message.TotalTransactions, Is.EqualTo(7));
            Assert.That(message.Hashes.Count, Is.EqualTo(4));
            Assert.That(message.Flags, Is.EqualTo(new byte[] {0x1D}));

            byte[] outBytes = BitcoinStreamWriter.GetBytes(message.Write);
            Assert.That(outBytes, Is.EqualTo(inBytes));
        }
    }
}