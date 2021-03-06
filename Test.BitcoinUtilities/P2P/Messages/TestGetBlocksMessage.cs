﻿using System.IO;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P.Messages
{
    [TestFixture]
    public class TestGetBlocksMessage
    {
        [Test]
        public void Test()
        {
            byte[] inBytes = new byte[]
                             {
                                 0x71, 0x11, 0x01, 0x00,
                                 0x01,
                                 0x6F, 0xE2, 0x8C, 0x0A, 0xB6, 0xF1, 0xB3, 0x72, 0xC1, 0xA6, 0xA2, 0x46, 0xAE, 0x63, 0xF7, 0x4F,
                                 0x93, 0x1E, 0x83, 0x65, 0xE1, 0x5A, 0x08, 0x9C, 0x68, 0xD6, 0x19, 0x00, 0x00, 0x00, 0x00, 0x00,
                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                             };

            GetBlocksMessage message;

            MemoryStream inStream = new MemoryStream(inBytes);
            using (BitcoinStreamReader reader = new BitcoinStreamReader(inStream))
            {
                message = GetBlocksMessage.Read(reader);
            }

            Assert.That(message.ProtocolVersion, Is.EqualTo(70001));
            Assert.That(message.LocatorHashes.Count, Is.EqualTo(1));
            Assert.That(message.LocatorHashes[0], Is.EqualTo(new byte[]
                                                             {
                                                                 0x6F, 0xE2, 0x8C, 0x0A, 0xB6, 0xF1, 0xB3, 0x72, 0xC1, 0xA6, 0xA2, 0x46, 0xAE, 0x63, 0xF7, 0x4F,
                                                                 0x93, 0x1E, 0x83, 0x65, 0xE1, 0x5A, 0x08, 0x9C, 0x68, 0xD6, 0x19, 0x00, 0x00, 0x00, 0x00, 0x00
                                                             }));
            Assert.That(message.HashStop, Is.EqualTo(new byte[]
                                                     {
                                                         0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                         0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                                                     }));
            Assert.That(message.HashStop, Is.Not.Null);

            byte[] outBytes = BitcoinStreamWriter.GetBytes(message.Write);
            Assert.That(outBytes, Is.EqualTo(inBytes));
        }
    }
}