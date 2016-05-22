using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Storage;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Storage
{
    [TestFixture]
    public class TestBlockLocator
    {
        [Test]
        public void TestEmptyLocator()
        {
            Assert.That(new BlockLocator().GetHashes().Length, Is.EqualTo(0));
        }

        [Test]
        public void TestGetRequiredBlockHeights()
        {
            Assert.That(BlockLocator.GetRequiredBlockHeights(1), Is.EquivalentTo(CalculateExpectedHeights(1, 1)));
            Assert.That(BlockLocator.GetRequiredBlockHeights(2), Is.EquivalentTo(CalculateExpectedHeights(1, 2)));
            Assert.That(BlockLocator.GetRequiredBlockHeights(3), Is.EquivalentTo(CalculateExpectedHeights(1, 3)));

            Assert.That(BlockLocator.GetRequiredBlockHeights(15), Is.EquivalentTo(CalculateExpectedHeights(1, 15)));
            Assert.That(BlockLocator.GetRequiredBlockHeights(16), Is.EquivalentTo(CalculateExpectedHeights(1, 16)));
            Assert.That(BlockLocator.GetRequiredBlockHeights(17), Is.EquivalentTo(CalculateExpectedHeights(1, 17)));

            Assert.That(BlockLocator.GetRequiredBlockHeights(20), Is.EquivalentTo(CalculateExpectedHeights(1, 20)));

            Assert.That(BlockLocator.GetRequiredBlockHeights(63), Is.EquivalentTo(CalculateExpectedHeights(1, 63)));
            Assert.That(BlockLocator.GetRequiredBlockHeights(64), Is.EquivalentTo(CalculateExpectedHeights(1, 64)));
            Assert.That(BlockLocator.GetRequiredBlockHeights(65), Is.EquivalentTo(CalculateExpectedHeights(1, 65)));

            Assert.That(BlockLocator.GetRequiredBlockHeights(299000), Is.EquivalentTo(CalculateExpectedHeights(1, 299000)));
            Assert.That(BlockLocator.GetRequiredBlockHeights(299001), Is.EquivalentTo(CalculateExpectedHeights(1, 299001)));
            Assert.That(BlockLocator.GetRequiredBlockHeights(299002), Is.EquivalentTo(CalculateExpectedHeights(1, 299002)));
        }

        [Test]
        public void TestTruncation()
        {
            BlockLocator locator = new BlockLocator();

            for (int i = 1; i <= 20; i++)
            {
                locator.AddHash(i, BitConverter.GetBytes(i));
            }

            Assert.That(locator.GetHashes().Select(val => BitConverter.ToInt32(val, 0)), Is.EqualTo(new int[]
            {
                20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 8, 4
            }));

            locator.AddHash(16, BitConverter.GetBytes(10016));

            Assert.That(locator.GetHashes().Select(val => BitConverter.ToInt32(val, 0)), Is.EqualTo(new int[]
            {
                10016, 15, 14, 13, 12, 11, 8, 4
            }));
        }

        [Test]
        public void TestSets()
        {
            for (int from = 1; from <= 33; from++)
            {
                for (int to = from; to <= 33; to++)
                {
                    TestSet(from, to);
                    TestSet(100 + from, 100 + to);
                    TestSet(1000 + from, 1000 + to);
                    TestSet(10000 + from, 10000 + to);
                    TestSet(100000 + from, 100000 + to);
                    TestSet(1000000 + from, 1000000 + to);
                    TestSet(10000000 + from, 10000000 + to);
                }
            }

            TestSet(1, 299000);
            TestSet(1, 299001);

            TestSet(100000, 299000);
            TestSet(100000, 299001);
        }

        private void TestSet(int from, int to)
        {
            BlockLocator locator = new BlockLocator();

            for (int i = from; i <= to; i++)
            {
                locator.AddHash(i, BitConverter.GetBytes(i));
            }

            Assert.That(
                locator.GetHashes().Select(val => BitConverter.ToInt32(val, 0)).ToList(),
                Is.EqualTo(CalculateExpectedHeights(from, to)),
                $"Testing set from {from} to {to}.");
        }

        private static List<int> CalculateExpectedHeights(int minHeight, int maxHeight)
        {
            SortedSet<int> expectedSet = new SortedSet<int>();
            int[] divisors = new int[] {1, 4, 16, 64, 256, 1024, 4096, 16384, 65536, 262144};
            foreach (int divisor in divisors)
            {
                int baseVal = maxHeight/divisor*divisor;
                for (int i = 0; i < 10; i++)
                {
                    int val = baseVal - i*divisor;
                    if (val >= minHeight)
                    {
                        expectedSet.Add(val);
                    }
                }
            }

            List<int> expectedList = expectedSet.ToList();
            expectedList.Reverse();
            return expectedList;
        }
    }
}