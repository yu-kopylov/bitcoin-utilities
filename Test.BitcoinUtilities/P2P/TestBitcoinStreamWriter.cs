using System.Net;
using BitcoinUtilities.P2P;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    public class TestBitcoinStreamWriter
    {
        [Test]
        public void TestWriteBigEndianUShort()
        {
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteBigEndian((ushort) 0x0000)), Is.EqualTo(new byte[] {0x00, 0x00}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteBigEndian((ushort) 0x00FF)), Is.EqualTo(new byte[] {0x00, 0xFF}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteBigEndian((ushort) 0x1234)), Is.EqualTo(new byte[] {0x12, 0x34}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteBigEndian((ushort) 0x7FFF)), Is.EqualTo(new byte[] {0x7F, 0xFF}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteBigEndian((ushort) 0xFF00)), Is.EqualTo(new byte[] {0xFF, 0x00}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteBigEndian((ushort) 0xFFFF)), Is.EqualTo(new byte[] {0xFF, 0xFF}));
        }

        [Test]
        public void TestWriteCompact()
        {
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0)), Is.EqualTo(new byte[] {0x00}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0x01)), Is.EqualTo(new byte[] {0x01}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0xFC)), Is.EqualTo(new byte[] {0xFC}));

            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0x00FD)), Is.EqualTo(new byte[] {0xFD, 0xFD, 0x00}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0x1234)), Is.EqualTo(new byte[] {0xFD, 0x34, 0x12}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0x7FFF)), Is.EqualTo(new byte[] {0xFD, 0xFF, 0x7F}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0xFFFF)), Is.EqualTo(new byte[] {0xFD, 0xFF, 0xFF}));

            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0x00010000)), Is.EqualTo(new byte[] {0xFE, 0x00, 0x00, 0x01, 0x00}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0x12345678)), Is.EqualTo(new byte[] {0xFE, 0x78, 0x56, 0x34, 0x12}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0x7FFFFFFF)), Is.EqualTo(new byte[] {0xFE, 0xFF, 0xFF, 0xFF, 0x7F}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0xFFFFFFFF)), Is.EqualTo(new byte[] {0xFE, 0xFF, 0xFF, 0xFF, 0xFF}));

            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0x0000000100000000)),
                Is.EqualTo(new byte[] {0xFF, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0x1234567890123456)),
                Is.EqualTo(new byte[] {0xFF, 0x56, 0x34, 0x12, 0x90, 0x78, 0x56, 0x34, 0x12}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0x7FFFFFFFFFFFFFFF)),
                Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0xFFFFFFFFFFFFFFFF)),
                Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF}));
        }

        [Test]
        public void TestWriteText()
        {
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteText("")), Is.EqualTo(new byte[] {0x00}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteText("1")), Is.EqualTo(new byte[] {0x01, 0x31}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteText("12")), Is.EqualTo(new byte[] {0x02, 0x31, 0x32}));

            byte[] expectedResult = new byte[256 + 3];
            expectedResult[0] = 0xFD;
            expectedResult[1] = 0x00;
            expectedResult[2] = 0x01;
            for (int i = 0; i < 256; i++)
            {
                expectedResult[i + 3] = 0x20;
            }
            expectedResult[expectedResult.Length - 2] = 0x31;
            expectedResult[expectedResult.Length - 1] = 0x32;
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteText("12".PadLeft(256))), Is.EqualTo(expectedResult));
        }

        [Test]
        public void TestWriteAddress()
        {
            Assert.That(
                BitcoinStreamWriter.GetBytes(r => r.WriteAddress(IPAddress.Parse("192.168.0.1"))),
                Is.EqualTo(new byte[]
                {
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xC0, 0xA8, 0x00, 0x01
                }));
            Assert.That(
                BitcoinStreamWriter.GetBytes(r => r.WriteAddress(IPAddress.Parse("2001:cdba::3257:9652"))),
                Is.EqualTo(new byte[]
                {
                    0x20, 0x01, 0xCD, 0xBA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x32, 0x57, 0x96, 0x52
                }));
        }
    }
}