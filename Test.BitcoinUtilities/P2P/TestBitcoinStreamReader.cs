using System;
using System.IO;
using System.Net;
using BitcoinUtilities.P2P;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    public class TestBitcoinStreamReader
    {
        [Test]
        public void TestReadUInt16BigEndian()
        {
            Assert.That(ExecuteRead(r => r.ReadUInt16BigEndian(), new byte[] {0x12, 0x34}), Is.EqualTo(0x1234));
            Assert.That(ExecuteRead(r => r.ReadUInt16BigEndian(), new byte[] {0x00, 0xFF}), Is.EqualTo(0x00FF));
            Assert.That(ExecuteRead(r => r.ReadUInt16BigEndian(), new byte[] {0xFF, 0x00}), Is.EqualTo(0xFF00));
        }

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

        [Test]
        public void TestReadText()
        {
            byte[] testString = new byte[] {0x04, (byte) 't', (byte) 'e', (byte) 's', (byte) 't'};

            Assert.That(ExecuteRead(r => r.ReadText(4), testString), Is.EqualTo("test"));
            Assert.Throws<IOException>(() => ExecuteRead(r => r.ReadText(3), testString));
        }

        [Test]
        public void TestReadAddress()
        {
            Assert.That(ExecuteRead(
                r => r.ReadAddress(),
                new byte[]
                {
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xC0, 0xA8, 0x00, 0x01
                }),
                Is.EqualTo(IPAddress.Parse("192.168.0.1").MapToIPv6()));

            Assert.That(
                ExecuteRead(r => r.ReadAddress(),
                    new byte[]
                    {
                        0x20, 0x01, 0xCD, 0xBA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x32, 0x57, 0x96, 0x52
                    }),
                Is.EqualTo(IPAddress.Parse("2001:cdba::3257:9652")));
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