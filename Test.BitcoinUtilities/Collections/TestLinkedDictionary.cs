using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Collections;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Collections
{
    [TestFixture]
    public class TestLinkedDictionary
    {
        [Test]
        public void Test()
        {
            LinkedDictionary<int, string> dict = new LinkedDictionary<int, string>(new ModComparer(10));

            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.That(dict.Select(p => p.Key).ToList(), Is.EqualTo(new int[] {}));
            Assert.That(dict.Select(p => p.Value).ToList(), Is.EqualTo(new string[] {}));

            dict.Add(1, "1");
            dict.Add(2, "2");
            dict.Add(3, "3");

            Assert.That(dict.Count, Is.EqualTo(3));
            Assert.That(dict.Select(p => p.Key).ToList(), Is.EqualTo(new int[] {1, 2, 3}));
            Assert.That(dict.Select(p => p.Value).ToList(), Is.EqualTo(new string[] {"1", "2", "3"}));

            dict.Clear();

            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.That(dict.Select(p => p.Key).ToList(), Is.EqualTo(new int[] {}));
            Assert.That(dict.Select(p => p.Value).ToList(), Is.EqualTo(new string[] {}));

            dict.Add(11, "11");
            dict.Add(12, "12");
            dict.Add(13, "13");

            Assert.That(dict.Count, Is.EqualTo(3));
            Assert.That(dict.Select(p => p.Key).ToList(), Is.EqualTo(new int[] {11, 12, 13}));
            Assert.That(dict.Select(p => p.Value).ToList(), Is.EqualTo(new string[] {"11", "12", "13"}));

            Assert.Throws<ArgumentException>(() => dict.Add(1, "1"));
            Assert.Throws<ArgumentException>(() => dict.Add(2, "2"));
            Assert.Throws<ArgumentException>(() => dict.Add(3, "3"));

            Assert.That(dict.Count, Is.EqualTo(3));
            Assert.That(dict.Select(p => p.Key).ToList(), Is.EqualTo(new int[] {11, 12, 13}));
            Assert.That(dict.Select(p => p.Value).ToList(), Is.EqualTo(new string[] {"11", "12", "13"}));

            Assert.True(dict.Remove(12));

            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict.Select(p => p.Key).ToList(), Is.EqualTo(new int[] {11, 13}));
            Assert.That(dict.Select(p => p.Value).ToList(), Is.EqualTo(new string[] {"11", "13"}));

            Assert.False(dict.Remove(12));

            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict.Select(p => p.Key).ToList(), Is.EqualTo(new int[] {11, 13}));
            Assert.That(dict.Select(p => p.Value).ToList(), Is.EqualTo(new string[] {"11", "13"}));

            dict.Add(12, "12");

            Assert.That(dict.Count, Is.EqualTo(3));
            Assert.That(dict.Select(p => p.Key).ToList(), Is.EqualTo(new int[] {11, 13, 12}));
            Assert.That(dict.Select(p => p.Value).ToList(), Is.EqualTo(new string[] {"11", "13", "12"}));

            Assert.That(dict[1], Is.EqualTo("11"));
            Assert.That(dict[2], Is.EqualTo("12"));
            Assert.That(dict[3], Is.EqualTo("13"));
            Assert.That(dict[101], Is.EqualTo("11"));
            Assert.That(dict[102], Is.EqualTo("12"));
            Assert.That(dict[103], Is.EqualTo("13"));

            dict[1] = "1";

            Assert.That(dict.Count, Is.EqualTo(3));
            Assert.That(dict.Select(p => p.Key).ToList(), Is.EqualTo(new int[] {11, 13, 12}));
            Assert.That(dict.Select(p => p.Value).ToList(), Is.EqualTo(new string[] {"1", "13", "12"}));

            Assert.That(dict[1], Is.EqualTo("1"));
            Assert.That(dict[2], Is.EqualTo("12"));
            Assert.That(dict[3], Is.EqualTo("13"));

            dict[14] = "14";
            Assert.That(dict.Count, Is.EqualTo(4));
            Assert.That(dict.Select(p => p.Key).ToList(), Is.EqualTo(new int[] {11, 13, 12, 14}));
            Assert.That(dict.Select(p => p.Value).ToList(), Is.EqualTo(new string[] {"1", "13", "12", "14"}));

            Assert.That(dict[1], Is.EqualTo("1"));
            Assert.That(dict[2], Is.EqualTo("12"));
            Assert.That(dict[3], Is.EqualTo("13"));
            Assert.That(dict[4], Is.EqualTo("14"));
        }

        [Test]
        public void TestExceptions()
        {
            LinkedDictionary<string, string> dict = new LinkedDictionary<string, string>();

            dict.Add("a", "a");

            Assert.Throws<ArgumentNullException>(() => dict.Add(null, "null"));
            Assert.Throws<ArgumentException>(() => dict.Add("a", "a"));

            Assert.Throws<ArgumentNullException>(() => dict.Remove(null));

            Assert.Throws<ArgumentNullException>(() => dict[null] = "null");

            Assert.Throws<ArgumentNullException>(() => Console.WriteLine(dict[null]));
            Assert.Throws<KeyNotFoundException>(() => Console.WriteLine(dict["b"]));
        }

        private class ModComparer : IEqualityComparer<int>
        {
            private readonly int modulus;

            public ModComparer(int modulus)
            {
                this.modulus = modulus;
            }

            public bool Equals(int x, int y)
            {
                return x%modulus == y%modulus;
            }

            public int GetHashCode(int obj)
            {
                return obj%modulus;
            }
        }
    }
}