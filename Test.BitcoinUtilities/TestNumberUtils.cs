using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestNumberUtils
    {
        [Test]
        public void Test()
        {
            Assert.That(NumberUtils.ReverseBytes(0x12345678), Is.EqualTo(0x78563412));
        }
    }
}