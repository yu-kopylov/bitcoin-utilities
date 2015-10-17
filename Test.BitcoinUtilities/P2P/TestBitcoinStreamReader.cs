using System;
using System.IO;
using BitcoinUtilities.P2P;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    public class TestBitcoinStreamReader
    {
        [Test]
        public void TestReadInt32BigEndian()
        {
            Assert.That(ExecuteRead(r => r.ReadInt32BigEndian(), new byte[] {0x12, 0x34, 0x56, 0x78}), Is.EqualTo(0x12345678));
        }

        [Test]
        public void TestReadUInt64Compact()
        {
            Assert.That(ExecuteRead(r => r.ReadUInt64Compact(), new byte[] {0x00}), Is.EqualTo(0));
            Assert.That(ExecuteRead(r => r.ReadUInt64Compact(), new byte[] {0x01}), Is.EqualTo(0x01));
            Assert.That(ExecuteRead(r => r.ReadUInt64Compact(), new byte[] {0xFC}), Is.EqualTo(0xFC));

            Assert.That(ExecuteRead(r => r.ReadUInt64Compact(), new byte[] {0xFD, 0xFD, 0x00}), Is.EqualTo(0x00FD));
            Assert.That(ExecuteRead(r => r.ReadUInt64Compact(), new byte[] {0xFD, 0x34, 0x12}), Is.EqualTo(0x1234));
            Assert.That(ExecuteRead(r => r.ReadUInt64Compact(), new byte[] {0xFD, 0xFF, 0x7F}), Is.EqualTo(0x7FFF));
            Assert.That(ExecuteRead(r => r.ReadUInt64Compact(), new byte[] {0xFD, 0xFF, 0xFF}), Is.EqualTo(0xFFFF));

            Assert.That(ExecuteRead(r => r.ReadUInt64Compact(), new byte[] {0xFE, 0x00, 0x00, 0x01, 0x00}), Is.EqualTo(0x00010000));
            Assert.That(ExecuteRead(r => r.ReadUInt64Compact(), new byte[] {0xFE, 0x78, 0x56, 0x34, 0x12}), Is.EqualTo(0x12345678));
            Assert.That(ExecuteRead(r => r.ReadUInt64Compact(), new byte[] {0xFE, 0xFF, 0xFF, 0xFF, 0x7F}), Is.EqualTo(0x7FFFFFFF));
            Assert.That(ExecuteRead(r => r.ReadUInt64Compact(), new byte[] {0xFE, 0xFF, 0xFF, 0xFF, 0xFF}), Is.EqualTo(0xFFFFFFFF));

            Assert.That(ExecuteRead(r => r.ReadUInt64Compact(), new byte[] {0xFF, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00}), Is.EqualTo(0x0000000100000000));
            Assert.That(ExecuteRead(r => r.ReadUInt64Compact(), new byte[] {0xFF, 0x56, 0x34, 0x12, 0x90, 0x78, 0x56, 0x34, 0x12}), Is.EqualTo(0x1234567890123456));
            Assert.That(ExecuteRead(r => r.ReadUInt64Compact(), new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F}), Is.EqualTo(0x7FFFFFFFFFFFFFFF));
            Assert.That(ExecuteRead(r => r.ReadUInt64Compact(), new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF}), Is.EqualTo(0xFFFFFFFFFFFFFFFF));
        }

        private T ExecuteRead<T>(Func<BitcoinStreamReader, T> readMethod, byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            using (BitcoinStreamReader reader = new BitcoinStreamReader(stream))
            {
                return readMethod(reader);
            }
        }
    }
}