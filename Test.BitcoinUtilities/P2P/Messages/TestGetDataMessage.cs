using System.IO;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P.Messages
{
    [TestFixture]
    public class TestGetDataMessage
    {
        [Test]
        public void Test()
        {
            byte[] inBytes = new byte[]
                             {
                                 0x01,
                                 0x03, 0x00, 0x00, 0x00,
                                 0x48, 0x60, 0xEB, 0x18, 0xBF, 0x1B, 0x16, 0x20, 0xE3, 0x7E, 0x94, 0x90, 0xFC, 0x8A, 0x42, 0x75,
                                 0x14, 0x41, 0x6F, 0xD7, 0x51, 0x59, 0xAB, 0x86, 0x68, 0x8E, 0x9A, 0x83, 0x00, 0x00, 0x00, 0x00
                             };

            GetDataMessage message;

            MemoryStream inStream = new MemoryStream(inBytes);
            using (BitcoinStreamReader reader = new BitcoinStreamReader(inStream))
            {
                message = GetDataMessage.Read(reader);
            }

            Assert.That(message.Inventory.Count, Is.EqualTo(1));
            Assert.That(message.Inventory[0].Type, Is.EqualTo(InventoryVectorType.MsgFilteredBlock));
            Assert.That(message.Inventory[0].Hash, Is.EqualTo(new byte[]
                                                              {
                                                                  0x48, 0x60, 0xEB, 0x18, 0xBF, 0x1B, 0x16, 0x20, 0xE3, 0x7E, 0x94, 0x90, 0xFC, 0x8A, 0x42, 0x75,
                                                                  0x14, 0x41, 0x6F, 0xD7, 0x51, 0x59, 0xAB, 0x86, 0x68, 0x8E, 0x9A, 0x83, 0x00, 0x00, 0x00, 0x00
                                                              }));

            byte[] outBytes = BitcoinStreamWriter.GetBytes(message.Write);
            Assert.That(outBytes, Is.EqualTo(inBytes));
        }
    }
}