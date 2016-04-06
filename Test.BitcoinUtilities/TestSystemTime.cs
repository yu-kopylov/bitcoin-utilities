using System;
using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestSystemTime
    {
        [Test]
        public void Test()
        {
            DateTime date1 = SystemTime.UtcNow;

            SystemTime.Override(date1.AddDays(10));

            DateTime futureDate1 = SystemTime.UtcNow;
            DateTime futureDate2 = SystemTime.UtcNow;

            SystemTime.Reset();

            DateTime date2 = SystemTime.UtcNow;

            Assert.That(date1, Is.LessThanOrEqualTo(date2));

            Assert.That(futureDate1, Is.EqualTo(futureDate2));

            Assert.That(date1, Is.LessThanOrEqualTo(futureDate1));
            Assert.That(date2, Is.LessThanOrEqualTo(futureDate1));
        }
    }
}