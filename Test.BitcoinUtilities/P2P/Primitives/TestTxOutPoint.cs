using System.Collections.Generic;
using System.Text;
using BitcoinUtilities;
using BitcoinUtilities.P2P.Primitives;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P.Primitives
{
    [TestFixture]
    public class TestTxOutPoint
    {
        [Test]
        public void TestEquality()
        {
            byte[] h1A = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Hash1"));
            byte[] h1B = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Hash1"));
            byte[] h2A = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("Hash2"));

            Assert.That(h1A, Is.Not.SameAs(h1B));

            TxOutPoint p1A0 = new TxOutPoint(h1A, 0);
            TxOutPoint p1A1 = new TxOutPoint(h1A, 1);
            TxOutPoint p1B0 = new TxOutPoint(h1B, 0);
            TxOutPoint p2A0 = new TxOutPoint(h2A, 0);

            Assert.That(p1A0.Equals(p1B0), Is.True);
            Assert.That(p1A1.Equals(p1B0), Is.False);
            Assert.That(p1A0.Equals(p2A0), Is.False);
            Assert.That(p1A1.Equals(p2A0), Is.False);

            Assert.That(object.Equals(p1A0, p1B0), Is.True);
            Assert.That(object.Equals(p1A1, p1B0), Is.False);
            Assert.That(object.Equals(p1A0, p2A0), Is.False);
            Assert.That(object.Equals(p1A1, p2A0), Is.False);

            Assert.That(p1A0 == p1B0, Is.True);
            Assert.That(p1A1 == p1B0, Is.False);
            Assert.That(p1A0 == p2A0, Is.False);
            Assert.That(p1A1 == p2A0, Is.False);

            Assert.That(p1A0 != p1B0, Is.False);
            Assert.That(p1A1 != p1B0, Is.True);
            Assert.That(p1A0 != p2A0, Is.True);
            Assert.That(p1A1 != p2A0, Is.True);

            Assert.That(p1A0.GetHashCode() == p1B0.GetHashCode());
            Assert.That(p1A1.GetHashCode() != p1B0.GetHashCode());
            Assert.That(p1A0.GetHashCode() != p2A0.GetHashCode());
            Assert.That(p1A1.GetHashCode() != p2A0.GetHashCode());

            HashSet<TxOutPoint> hashSet = new HashSet<TxOutPoint>();
            Assert.That(hashSet.Add(p1A0), Is.True);
            Assert.That(hashSet.Add(p1A1), Is.True);
            Assert.That(hashSet.Add(p1B0), Is.False);
            Assert.That(hashSet.Add(p2A0), Is.True);
        }
    }
}