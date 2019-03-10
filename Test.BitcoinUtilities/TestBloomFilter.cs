using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestBloomFilter
    {
        // examples were taken from: https://github.com/bitcoin/bitcoin/blob/master/src/test/bloom_tests.cpp

        [Test]
        public void TestNoTweak()
        {
            BloomFilter filter = new BloomFilter(5, 0, new byte[3]);

            filter.Add(HexUtils.GetBytesUnsafe("99108ad8ed9bb6274d3980bab5a85c048f0950c8"));
            Assert.That(filter.Contains(HexUtils.GetBytesUnsafe("99108ad8ed9bb6274d3980bab5a85c048f0950c8")), Is.True);
            Assert.That(filter.Contains(HexUtils.GetBytesUnsafe("19108ad8ed9bb6274d3980bab5a85c048f0950c8")), Is.False);

            filter.Add(HexUtils.GetBytesUnsafe("b5a2c786d9ef4658287ced5914b37a1b4aa32eee"));
            Assert.That(filter.Contains(HexUtils.GetBytesUnsafe("b5a2c786d9ef4658287ced5914b37a1b4aa32eee")), Is.True);

            filter.Add(HexUtils.GetBytesUnsafe("b9300670b4c5366e95b2699e8b18bc75e5f729c5"));
            Assert.That(filter.Contains(HexUtils.GetBytesUnsafe("b9300670b4c5366e95b2699e8b18bc75e5f729c5")), Is.True);

            Assert.That(HexUtils.GetString(filter.Bits), Is.EqualTo("614e9b"));
        }

        [Test]
        public void TestWithTweak()
        {
            BloomFilter filter = new BloomFilter(5, 0x80000001, new byte[3]);

            filter.Add(HexUtils.GetBytesUnsafe("99108ad8ed9bb6274d3980bab5a85c048f0950c8"));
            Assert.That(filter.Contains(HexUtils.GetBytesUnsafe("99108ad8ed9bb6274d3980bab5a85c048f0950c8")), Is.True);
            Assert.That(filter.Contains(HexUtils.GetBytesUnsafe("19108ad8ed9bb6274d3980bab5a85c048f0950c8")), Is.False);

            filter.Add(HexUtils.GetBytesUnsafe("b5a2c786d9ef4658287ced5914b37a1b4aa32eee"));
            Assert.That(filter.Contains(HexUtils.GetBytesUnsafe("b5a2c786d9ef4658287ced5914b37a1b4aa32eee")), Is.True);

            filter.Add(HexUtils.GetBytesUnsafe("b9300670b4c5366e95b2699e8b18bc75e5f729c5"));
            Assert.That(filter.Contains(HexUtils.GetBytesUnsafe("b9300670b4c5366e95b2699e8b18bc75e5f729c5")), Is.True);

            Assert.That(HexUtils.GetString(filter.Bits), Is.EqualTo("ce4299"));
        }
    }
}