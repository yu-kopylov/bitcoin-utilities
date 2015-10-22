using BitcoinUtilities.P2P;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    public class TestBitcoinStreamWriter
    {
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

            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0x0000000100000000)), Is.EqualTo(new byte[] {0xFF, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0x1234567890123456)), Is.EqualTo(new byte[] {0xFF, 0x56, 0x34, 0x12, 0x90, 0x78, 0x56, 0x34, 0x12}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0x7FFFFFFFFFFFFFFF)), Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F}));
            Assert.That(BitcoinStreamWriter.GetBytes(r => r.WriteCompact(0xFFFFFFFFFFFFFFFF)), Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF}));
        }
    }
}