using System.IO;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P.Messages
{
    [TestFixture]
    public class TestHeadersMessage
    {
        [Test]
        public void Test()
        {
            byte[] inBytes = new byte[]
            {
                //Header count: 1
                0x01,
                // Block version: 2
                0x02, 0x00, 0x00, 0x00,
                // Hash of previous block's header
                0xB6, 0xFF, 0x0B, 0x1B, 0x16, 0x80, 0xA2, 0x86, 0x2A, 0x30, 0xCA, 0x44, 0xD3, 0x46, 0xD9, 0xE8,
                0x91, 0x0D, 0x33, 0x4B, 0xEB, 0x48, 0xCA, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // Merkle root
                0x9D, 0x10, 0xAA, 0x52, 0xEE, 0x94, 0x93, 0x86, 0xCA, 0x93, 0x85, 0x69, 0x5F, 0x04, 0xED, 0xE2,
                0x70, 0xDD, 0xA2, 0x08, 0x10, 0xDE, 0xCD, 0x12, 0xBC, 0x9B, 0x04, 0x8A, 0xAA, 0xB3, 0x14, 0x71,
                // Unix time: 1415239972
                0x24, 0xD9, 0x5A, 0x54,
                // Target (nBits)
                0x30, 0xC3, 0x1B, 0x18,
                // Nonce
                0xFE, 0x9F, 0x08, 0x64,
                // Transaction count (0x00)
                0x00
            };

            HeadersMessage message;

            MemoryStream inStream = new MemoryStream(inBytes);
            using (BitcoinStreamReader reader = new BitcoinStreamReader(inStream))
            {
                message = HeadersMessage.Read(reader);
            }

            Assert.That(message.Headers.Length, Is.EqualTo(1));
            Assert.That(message.Headers[0].Version, Is.EqualTo(2));
            Assert.That(message.Headers[0].Timestamp, Is.EqualTo(1415239972));
            Assert.That(message.Headers[0].NBits, Is.EqualTo(0x181BC330));

            byte[] outBytes = BitcoinStreamWriter.GetBytes(message.Write);
            Assert.That(outBytes, Is.EqualTo(inBytes));
        }
    }
}