using System.Linq;
using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestLongGuid
    {
        [Test]
        public void Test()
        {
            byte[] guid1 = LongGuid.NewGuid();
            byte[] guid2 = LongGuid.NewGuid();

            Assert.That(guid1.Length, Is.EqualTo(64));
            Assert.That(guid1.Count(b => b != 0), Is.GreaterThan(32));

            Assert.That(guid2.Length, Is.EqualTo(64));
            Assert.That(guid2.Count(b => b != 0), Is.GreaterThan(32));

            Assert.That(guid1, Is.Not.EqualTo(guid2));
        }
    }
}