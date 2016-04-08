using System;
using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestUnixTime
    {
        [Test]
        public void TestToTimeStamp()
        {
            Assert.That(UnixTime.ToTimestamp(new DateTime(2016, 4, 8, 13, 34, 35, DateTimeKind.Utc)), Is.EqualTo(1460122475));
        }

        [Test]
        public void TestToDateTime()
        {
            DateTime expected = new DateTime(2016, 4, 8, 13, 34, 35, DateTimeKind.Utc);
            DateTime actual = UnixTime.ToDateTime(1460122475);

            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.Kind, Is.EqualTo(expected.Kind));
        }
    }
}