using System.IO;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P.Messages
{
    [TestFixture]
    public class TestPingMessage
    {
        [Test]
        public void Test()
        {
            byte[] inBytes = new byte[]
            {
                0xEF, 0xCD, 0xAB, 0x90, 0x78, 0x56, 0x34, 0x12
            };

            PingMessage message;

            MemoryStream inStream = new MemoryStream(inBytes);
            using (BitcoinStreamReader reader = new BitcoinStreamReader(inStream))
            {
                message = PingMessage.Read(reader);
            }

            Assert.That(message.Nonce, Is.EqualTo(0x1234567890ABCDEF));

            byte[] outBytes = BitcoinStreamWriter.GetBytes(message.Write);
            Assert.That(outBytes, Is.EqualTo(inBytes));
        }
    }
}