using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestSecureRandom
    {
        [Test]
        public void TestWithEmptySeed()
        {
            SecureRandom random1 = SecureRandom.CreateEmpty();
            SecureRandom random2 = SecureRandom.CreateEmpty();

            byte[] value1 = random1.NextBytes(33);
            byte[] value2 = random2.NextBytes(33);

            Assert.That(value1.Length, Is.EqualTo(33));
            Assert.That(value2.Length, Is.EqualTo(33));

            Assert.That(value1, Is.EqualTo(value2));

            value1 = random1.NextBytes(1024);
            value2 = random2.NextBytes(1024);

            Assert.That(value1.Length, Is.EqualTo(1024));
            Assert.That(value2.Length, Is.EqualTo(1024));

            Assert.That(value1, Is.EqualTo(value2));
        }

        [Test]
        public void TestUniqueness()
        {
            SecureRandom random1 = SecureRandom.Create();
            SecureRandom random2 = SecureRandom.Create();

            byte[] value1 = random1.NextBytes(33);
            byte[] value2 = random2.NextBytes(33);

            Assert.That(value1.Length, Is.EqualTo(33));
            Assert.That(value2.Length, Is.EqualTo(33));

            Assert.That(value1, Is.Not.EqualTo(value2));

            value1 = random1.NextBytes(1024);
            value2 = random2.NextBytes(1024);

            Assert.That(value1.Length, Is.EqualTo(1024));
            Assert.That(value2.Length, Is.EqualTo(1024));

            Assert.That(value1, Is.Not.EqualTo(value2));
        }

        [Test]
        public void TestAddSeedMaterial()
        {
            SecureRandom random1 = SecureRandom.CreateEmpty();
            SecureRandom random2 = SecureRandom.CreateEmpty();

            random1.AddSeedMaterial(new byte[] {1});
            random2.AddSeedMaterial(new byte[] {2});

            byte[] value1 = random1.NextBytes(33);
            byte[] value2 = random2.NextBytes(33);

            Assert.That(value1, Is.Not.EqualTo(value2));

            value1 = random1.NextBytes(1024);
            value2 = random2.NextBytes(1024);

            Assert.That(value1, Is.Not.EqualTo(value2));
        }

        [Test]
        public void TestAddSameSeedMaterial()
        {
            SecureRandom random1 = SecureRandom.Create();
            SecureRandom random2 = SecureRandom.Create();

            random1.AddSeedMaterial(new byte[] {1});
            random2.AddSeedMaterial(new byte[] {1});

            byte[] value1 = random1.NextBytes(33);
            byte[] value2 = random2.NextBytes(33);

            Assert.That(value1, Is.Not.EqualTo(value2));

            value1 = random1.NextBytes(1024);
            value2 = random2.NextBytes(1024);

            Assert.That(value1, Is.Not.EqualTo(value2));

            random1 = SecureRandom.CreateEmpty();
            random2 = SecureRandom.CreateEmpty();

            random1.AddSeedMaterial(new byte[] {1});
            random2.AddSeedMaterial(new byte[] {1});

            value1 = random1.NextBytes(33);
            value2 = random2.NextBytes(33);

            Assert.That(value1, Is.EqualTo(value2));

            value1 = random1.NextBytes(1024);
            value2 = random2.NextBytes(1024);

            Assert.That(value1, Is.EqualTo(value2));
        }

        [Test]
        public void TestDistribution()
        {
            byte[] value = SecureRandom.Create().NextBytes(1000000);
            int[] counts = new int[256];
            foreach (byte b in value)
            {
                counts[b]++;
            }
            foreach (int count in counts)
            {
                Assert.That(count, Is.GreaterThan(40));
            }
        }
    }
}