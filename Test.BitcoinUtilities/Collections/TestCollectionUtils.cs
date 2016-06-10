using System;
using System.Collections.Generic;
using BitcoinUtilities.Collections;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Collections
{
    [TestFixture]
    public class TestCollectionUtils
    {
        [Test]
        public void TestErrors()
        {
            Assert.Throws<ArgumentException>(() => CollectionUtils.Split((List<int>) null, 1));
            Assert.Throws<ArgumentException>(() => CollectionUtils.Split(new int[0], -1));
            Assert.Throws<ArgumentException>(() => CollectionUtils.Split(new int[0], 0));
            Assert.Throws<ArgumentException>(() => CollectionUtils.Split(new int[10], -1));
            Assert.Throws<ArgumentException>(() => CollectionUtils.Split(new int[10], 0));
        }

        [Test]
        public void Test()
        {
            Assert.That(CollectionUtils.Split(new int[0], 1), Is.EqualTo(new List<List<int>>()));
            Assert.That(CollectionUtils.Split(new int[0], 10), Is.EqualTo(new List<List<int>>()));

            Assert.That(CollectionUtils.Split(new int[] {1}, 1), Is.EqualTo(new List<List<int>> {new List<int> {1}}));
            Assert.That(CollectionUtils.Split(new int[] {1, 2}, 1), Is.EqualTo(new List<List<int>>
            {
                new List<int> {1},
                new List<int> {2}
            }));
            Assert.That(CollectionUtils.Split(new int[] {1, 2}, 2), Is.EqualTo(new List<List<int>>
            {
                new List<int> {1, 2}
            }));
            Assert.That(CollectionUtils.Split(new int[] {1, 2, 3}, 2), Is.EqualTo(new List<List<int>>
            {
                new List<int> {1, 2},
                new List<int> {3}
            }));
            Assert.That(CollectionUtils.Split(new int[] {1, 2, 3, 4}, 2), Is.EqualTo(new List<List<int>>
            {
                new List<int> {1, 2},
                new List<int> {3, 4}
            }));
        }
    }
}