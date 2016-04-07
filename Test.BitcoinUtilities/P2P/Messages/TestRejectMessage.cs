using System.IO;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P.Messages
{
    [TestFixture]
    public class TestRejectMessage
    {
        [Test]
        public void Test()
        {
            byte[] inBytes = new byte[]
            {
                // type of message rejected: 'test'
                0x04, 0x74, 0x65, 0x73, 0x74,
                // reason: invalid
                0x10,
                // reason text: 'Unknown command.'
                0x10, 0x55, 0x6E, 0x6B, 0x6E, 0x6F, 0x77, 0x6E, 0x20, 0x63, 0x6F, 0x6D, 0x6D, 0x61, 0x6E, 0x64, 0x2E
            };

            RejectMessage message;

            MemoryStream inStream = new MemoryStream(inBytes);
            using (BitcoinStreamReader reader = new BitcoinStreamReader(inStream))
            {
                message = RejectMessage.Read(reader);
            }

            Assert.That(message.RejectedCommand, Is.EqualTo("test"));
            Assert.That(message.Reason, Is.EqualTo(RejectMessage.RejectReason.Invalid));
            Assert.That(message.ReasonText, Is.EqualTo("Unknown command."));
            Assert.That(message.Data, Is.Null);

            byte[] outBytes = BitcoinStreamWriter.GetBytes(message.Write);
            Assert.That(outBytes, Is.EqualTo(inBytes));
        }
    }
}