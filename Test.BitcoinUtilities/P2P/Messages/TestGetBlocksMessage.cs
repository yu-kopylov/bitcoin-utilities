using System.IO;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
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
                                 0x72, 0x11, 0x01, 0x00,
                                 0x01,
                                 0x67, 0xB8, 0x12, 0xE7, 0x09, 0x06, 0xB0, 0x33, 0x4C, 0x01, 0xED, 0x03, 0x5B, 0x50, 0xDB, 0x68,
                                 0xCD, 0xF3, 0xCF, 0x67, 0x20, 0xBD, 0xB8, 0xBF, 0xF5, 0x31, 0x34, 0xE7, 0xD6, 0x94, 0x6D, 0xA4,
                                 0xE4, 0x99, 0x3F, 0xAE, 0xCF, 0xC8, 0xFA, 0x6D, 0x1A, 0x75, 0x63, 0xE7, 0xA6, 0xCF, 0x1F, 0xC4,
                                 0xFA, 0x58, 0x5A, 0x5C, 0xC3, 0x65, 0x1F, 0x9F, 0x0B, 0xDD, 0xDA, 0xC0, 0x8F, 0x1F, 0xB9, 0x80
                             };

            GetBlocksMessage message;

            MemoryStream inStream = new MemoryStream(inBytes);
            using (BitcoinStreamReader reader = new BitcoinStreamReader(inStream))
            {
                message = GetBlocksMessage.Read(reader);
            }

            Assert.That(message.ProtocolVersion, Is.EqualTo(70002));
            Assert.That(message.LocatorHashes.Count, Is.EqualTo(1));
            Assert.That(message.LocatorHashes[0], Is.EqualTo(new byte[]
                                                             {
                                                                 0x67, 0xB8, 0x12, 0xE7, 0x09, 0x06, 0xB0, 0x33, 0x4C, 0x01, 0xED, 0x03, 0x5B, 0x50, 0xDB, 0x68,
                                                                 0xCD, 0xF3, 0xCF, 0x67, 0x20, 0xBD, 0xB8, 0xBF, 0xF5, 0x31, 0x34, 0xE7, 0xD6, 0x94, 0x6D, 0xA4
                                                             }));
            Assert.That(message.HashStop, Is.EqualTo(new byte[]
                                                     {
                                                         0xE4, 0x99, 0x3F, 0xAE, 0xCF, 0xC8, 0xFA, 0x6D, 0x1A, 0x75, 0x63, 0xE7, 0xA6, 0xCF, 0x1F, 0xC4,
                                                         0xFA, 0x58, 0x5A, 0x5C, 0xC3, 0x65, 0x1F, 0x9F, 0x0B, 0xDD, 0xDA, 0xC0, 0x8F, 0x1F, 0xB9, 0x80
                                                     }));
            Assert.That(message.HashStop, Is.Not.Null);

            MemoryStream outStream = new MemoryStream();
            using (BitcoinStreamWriter writer = new BitcoinStreamWriter(outStream))
            {
                message.Write(writer);
            }

            byte[] outBytes = outStream.ToArray();
            Assert.That(outBytes, Is.EqualTo(inBytes));
        }
    }
}