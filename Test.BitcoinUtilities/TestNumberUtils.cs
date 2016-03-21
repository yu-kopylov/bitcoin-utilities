using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestNumberUtils
    {
        [Test]
        public void TestReverseBytes()
        {
            Assert.That(NumberUtils.ReverseBytes(0x12345678), Is.EqualTo(0x78563412));
        }

        [Test]
        public void TestGetBytes()
        {
            Assert.That(NumberUtils.GetBytes(0), Is.EqualTo(new byte[] {0, 0, 0, 0}));
            Assert.That(NumberUtils.GetBytes(1), Is.EqualTo(new byte[] {1, 0, 0, 0}));
            Assert.That(NumberUtils.GetBytes(-1), Is.EqualTo(new byte[] {0xFF, 0xFF, 0xFF, 0xFF}));
            Assert.That(NumberUtils.GetBytes(0x12345678), Is.EqualTo(new byte[] {0x78, 0x56, 0x34, 0x12}));
            Assert.That(
                NumberUtils.GetBytes(unchecked((int) 0x81828384)),
                Is.EqualTo(new byte[] {0x84, 0x83, 0x82, 0x81}));
        }
    }
}