using System.IO;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P.Messages
{
    [TestFixture]
    public class TestInvMessage
    {
        [Test]
        public void Test()
        {
            byte[] inBytes = new byte[]
                           {
                               0x02,
                               0x01, 0x00, 0x00, 0x00,
                               0x67, 0xB8, 0x12, 0xE7, 0x09, 0x06, 0xB0, 0x33, 0x4C, 0x01, 0xED, 0x03, 0x5B, 0x50, 0xDB, 0x68,
                               0xCD, 0xF3, 0xCF, 0x67, 0x20, 0xBD, 0xB8, 0xBF, 0xF5, 0x31, 0x34, 0xE7, 0xD6, 0x94, 0x6D, 0xA4,
                               0x01, 0x00, 0x00, 0x00,
                               0xE4, 0x99, 0x3F, 0xAE, 0xCF, 0xC8, 0xFA, 0x6D, 0x1A, 0x75, 0x63, 0xE7, 0xA6, 0xCF, 0x1F, 0xC4,
                               0xFA, 0x58, 0x5A, 0x5C, 0xC3, 0x65, 0x1F, 0x9F, 0x0B, 0xDD, 0xDA, 0xC0, 0x8F, 0x1F, 0xB9, 0x80
                           };

            InvMessage message;

            MemoryStream inStream = new MemoryStream(inBytes);
            using (BitcoinStreamReader reader = new BitcoinStreamReader(inStream))
            {
                message = InvMessage.Read(reader);
            }

            Assert.That(message.Inventory.Count, Is.EqualTo(2));
            Assert.That(message.Inventory[0].Type, Is.EqualTo(InventoryVectorType.MsgTx));
            Assert.That(message.Inventory[1].Type, Is.EqualTo(InventoryVectorType.MsgTx));
            Assert.That(message.Inventory[0].Hash.Length, Is.EqualTo(32));
            Assert.That(message.Inventory[1].Hash.Length, Is.EqualTo(32));

            byte[] outBytes = BitcoinStreamWriter.GetBytes(message.Write);
            Assert.That(outBytes, Is.EqualTo(inBytes));
        }
    }
}