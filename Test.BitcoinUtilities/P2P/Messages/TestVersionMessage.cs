using System.IO;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P.Messages
{
    [TestFixture]
    class TestVersionMessage
    {
        [Test]
        public void Test()
        {
            byte[] inBytes = new byte[]
            {
                // 70002 (protocol version 70002)
                0x72, 0x11, 0x01, 0x00,
                // 1 (NODE_NETWORK services)
                0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // Tue Dec 18 10:12:33 PST 2012
                0x11, 0xB2, 0xD0, 0x50, 0x00, 0x00, 0x00, 0x00,
                // Recipient address info - see Network Address,
                0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // Sender address info - see Network Address
                0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // Node ID
                0x3B, 0x2E, 0xB3, 0x5D, 0x8C, 0xE6, 0x17, 0x65,
                // "/Satoshi:0.7.2/" sub-version string (string is 15 bytes long)
                0x0F, 0x2F, 0x53, 0x61, 0x74, 0x6F, 0x73, 0x68, 0x69, 0x3A, 0x30, 0x2E, 0x37, 0x2E, 0x32, 0x2F,
                // Last block sending node has is block #212672
                0xC0, 0x3E, 0x03, 0x00,
                // relay=true
                0x01
            };

            VersionMessage message;

            MemoryStream inStream = new MemoryStream(inBytes);
            using (BitcoinStreamReader reader = new BitcoinStreamReader(inStream))
            {
                message = VersionMessage.Read(reader);
            }

            Assert.That(message.ProtocolVersion, Is.EqualTo(70002));
            Assert.That(message.UserAgent, Is.EqualTo("/Satoshi:0.7.2/"));
            Assert.That(message.Services, Is.EqualTo(1));
            Assert.That(message.StartHeight, Is.EqualTo(212672));
            Assert.That(message.AcceptBroadcasts, Is.True);

            byte[] outBytes = BitcoinStreamWriter.GetBytes(message.Write);
            Assert.That(outBytes, Is.EqualTo(inBytes));
        }
    }
}