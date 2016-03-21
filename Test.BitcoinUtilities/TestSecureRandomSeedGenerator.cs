using System.Linq;
using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestSecureRandomSeedGenerator
    {
        [Test]
        public void Test()
        {
            byte[] seed1 = SecureRandomSeedGenerator.CreateSeed();
            byte[] seed2 = SecureRandomSeedGenerator.CreateSeed();

            Assert.That(seed1.Length, Is.EqualTo(64));
            Assert.That(seed1.Count(b => b != 0), Is.GreaterThan(32));

            Assert.That(seed2.Length, Is.EqualTo(64));
            Assert.That(seed2.Count(b => b != 0), Is.GreaterThan(32));

            Assert.That(seed1, Is.Not.EqualTo(seed2));
        }
    }
}