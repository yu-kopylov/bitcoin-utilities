using NUnit.Framework;
using System.Linq;
using BitcoinUtilities;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestBioRandom
    {
        [Test]
        public void TestEntropyEstimation()
        {
            BioRandom random = new BioRandom();

            random.AddPoint(100, 100);
            random.AddPoint(200, 200);

            int entropy = random.Entropy;
            Assert.That(entropy, Is.GreaterThan(0));

            random.AddPoint(200, 200);
            Assert.That(random.Entropy, Is.EqualTo(entropy));

            random.AddPoint(300, 200);
            Assert.That(random.Entropy, Is.GreaterThan(entropy));
            entropy = random.Entropy;

            random.AddPoint(300, 300);
            Assert.That(random.Entropy, Is.GreaterThan(entropy));
            entropy = random.Entropy;

            random.AddPoint(200, 300);
            Assert.That(random.Entropy, Is.GreaterThan(entropy));
            entropy = random.Entropy;

            random.AddPoint(200, 200);
            Assert.That(random.Entropy, Is.GreaterThan(entropy));

            byte[] material = random.CreateSeedMaterial();
            Assert.That(material.Length, Is.EqualTo(64));
            Assert.That(material.Count(b => b != 0), Is.GreaterThan(32));

            Assert.That(random.Entropy, Is.EqualTo(0));
        }

        [Test]
        public void TestDifferentInputs()
        {
            BioRandom random2 = new BioRandom();
            BioRandom random1 = new BioRandom();

            random1.AddPoint(100, 100);
            random2.AddPoint(100, 100);

            random1.AddPoint(200, 200);
            random2.AddPoint(300, 300);

            byte[] material1 = random1.CreateSeedMaterial();
            byte[] material2 = random2.CreateSeedMaterial();

            Assert.That(material1.Length, Is.EqualTo(64));
            Assert.That(material1.Count(b => b != 0), Is.GreaterThan(32));

            Assert.That(material2.Length, Is.EqualTo(64));
            Assert.That(material2.Count(b => b != 0), Is.GreaterThan(32));

            int matchCount = 0;
            for (int i = 0; i < material1.Length; i++)
            {
                if (material1[i] == material2[i])
                {
                    matchCount++;
                }
            }

            Assert.That(matchCount, Is.LessThan(16));
        }
    }
}