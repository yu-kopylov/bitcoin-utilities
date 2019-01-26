using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Threading;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities.Threading
{
    [TestFixture]
    public class TestConcurrentEnumerator
    {
        [Test]
        public void TestNullAndEmpty()
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => new ConcurrentEnumerator<int>(null)).ParamName, Is.EqualTo("collection"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => new ConcurrentEnumerator<string>(null)).ParamName, Is.EqualTo("collection"));

            var enumerator = new ConcurrentEnumerator<int>(new int[0]);

            int value;

            Assert.That(enumerator.GetNext(out value), Is.False);
            Assert.That(value, Is.EqualTo(0));

            Assert.That(enumerator.GetNext(out value), Is.False);
            Assert.That(value, Is.EqualTo(0));
        }

        [Test]
        public void TestEnumeration()
        {
            var enumerator = new ConcurrentEnumerator<string>(new string[] {"a10", "a20", "a30"});

            string value;

            Assert.That(enumerator.GetNext(out value), Is.True);
            Assert.That(value, Is.EqualTo("a10"));

            Assert.That(enumerator.GetNext(out value), Is.True);
            Assert.That(value, Is.EqualTo("a20"));

            Assert.That(enumerator.GetNext(out value), Is.True);
            Assert.That(value, Is.EqualTo("a30"));

            Assert.That(enumerator.GetNext(out value), Is.False);
            Assert.That(value, Is.Null);

            Assert.That(enumerator.GetNext(out value), Is.False);
            Assert.That(value, Is.Null);
        }

        [Test]
        public void TestConcurrency()
        {
            const int collectionSize = 1_000_000;
            int[] collection = Enumerable.Range(1, collectionSize).ToArray();
            Assert.That(collection.Length, Is.EqualTo(collectionSize));

            ConcurrentBag<long> sums = new ConcurrentBag<long>();
            var enumerator = new ConcurrentEnumerator<int>(collection);

            const int threadCount = 10;
            TestUtils.TestConcurrency(threadCount, 1, (t, i) =>
            {
                long sum = 0;
                while (enumerator.GetNext(out var value))
                {
                    sum += value;
                }

                sums.Add(sum);
                return true;
            });

            long expectedSum = (1 + collectionSize) * (long) collectionSize / 2;
            Assert.That(sums.Count, Is.EqualTo(threadCount));
            Assert.That(sums.Sum(), Is.EqualTo(expectedSum));
        }

        [Test]
        public void TestProcessWith()
        {
            const int collectionSize = 1_000_000;
            int[] collection = Enumerable.Range(1, collectionSize).ToArray();
            Assert.That(collection.Length, Is.EqualTo(collectionSize));
            var enumerator = new ConcurrentEnumerator<int>(collection);

            const int threadCount = 10;
            int[] resources = Enumerable.Range(1, threadCount).ToArray();
            List<(int thread, long sum)> results = enumerator.ProcessWith(resources, (resource, en) =>
            {
                long sum = 0;

                while (en.GetNext(out var value))
                {
                    sum += value;
                }

                return (resource, sum);
            });

            long expectedSum = (1 + collectionSize) * (long) collectionSize / 2;
            Assert.That(results.Count, Is.EqualTo(threadCount));
            Assert.That(results.Select(r => r.thread), Is.EquivalentTo(Enumerable.Range(1, threadCount)));
            Assert.That(results.Sum(p => p.sum), Is.EqualTo(expectedSum));
        }

        [Test]
        public void TestProcessWithException()
        {
            int[] collection = Enumerable.Range(1, 10).ToArray();
            var enumerator = new ConcurrentEnumerator<int>(collection);

            int[] resources = Enumerable.Range(1, 10).ToArray();
            var exception = Assert.Throws<AggregateException>(() => enumerator.ProcessWith(resources, (resource, en) =>
            {
                while (en.GetNext(out var value))
                {
                    if (value == 6 || value == 7)
                    {
                        throw new Exception($"Test exception on {value}.");
                    }
                }

                return 0;
            }));

            Assert.That(exception.InnerExceptions.Select(e => e.Message), Is.EquivalentTo(new string[] {"Test exception on 6.", "Test exception on 7."}));
        }
    }
}