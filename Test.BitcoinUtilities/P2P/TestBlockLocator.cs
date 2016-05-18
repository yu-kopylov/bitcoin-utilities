using System;
using System.Linq;
using BitcoinUtilities.P2P;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    public class TestBlockLocator
    {
        [Test]
        public void TestSmoke()
        {
            BlockLocator locator = new BlockLocator();

            int[] requiredHeights = locator.GetRequiredBlockHeights(20);
            Assert.That(requiredHeights, Is.EquivalentTo(new int[] {4, 8, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20}));

            for (int i = 1; i <= 20; i++)
            {
                locator.AddHash(i, BitConverter.GetBytes(i));
            }

            byte[][] locatorHashes = locator.GetHashes();
            Assert.That(locatorHashes, Is.EqualTo(new int[] {20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 8, 4}.Select(BitConverter.GetBytes)));

            //todo: add more tests
        }
    }
}