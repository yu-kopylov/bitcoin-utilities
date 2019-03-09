using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestReferenceEqualityComparer
    {
        [Test]
        public void Test()
        {
            const string s1 = "1";
            const string s3 = "3";
            const string s12 = "12";
            const string s23 = "23";
            const string s123 = "123";

            string s12_3 = Concat(s12, s3);
            string s1_23 = Concat(s1, s23);

            Assert.That(s123, Is.EqualTo(s1_23), "sanity check");
            Assert.That(s123, Is.EqualTo(s12_3), "sanity check");

            var comparer = ReferenceEqualityComparer<string>.Instance;

            Assert.That(comparer.Equals(s123, s123), Is.True);
            Assert.That(comparer.Equals(s1_23, s1_23), Is.True);
            Assert.That(comparer.Equals(s12_3, s12_3), Is.True);

            Assert.That(comparer.Equals(s123, s1_23), Is.False);
            Assert.That(comparer.Equals(s123, s12_3), Is.False);
            Assert.That(comparer.Equals(s1_23, s12_3), Is.False);

            Assert.That(comparer.GetHashCode(s123), Is.EqualTo(comparer.GetHashCode(s123)));
            Assert.That(comparer.GetHashCode(s1_23), Is.EqualTo(comparer.GetHashCode(s1_23)));
            Assert.That(comparer.GetHashCode(s12_3), Is.EqualTo(comparer.GetHashCode(s12_3)));

            Assert.That(
                comparer.GetHashCode(s123) != comparer.GetHashCode(s1_23) ||
                comparer.GetHashCode(s123) != comparer.GetHashCode(s12_3) ||
                comparer.GetHashCode(s1_23) != comparer.GetHashCode(s12_3),
                "This condition is not guaranteed, but it is highly unlikely to fail with correct implementation of GetHashCode."
            );
        }

        private static string Concat(string a, string b)
        {
            return a + b;
        }
    }
}