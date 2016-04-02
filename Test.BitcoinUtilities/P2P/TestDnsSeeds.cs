using System.Collections.Generic;
using System.Net;
using BitcoinUtilities.P2P;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    public class TestDnsSeeds
    {
        [Test]
        public void Test()
        {
            List<IPAddress> addresses = DnsSeeds.GetNodeAddresses();
            Assert.That(addresses.Count, Is.GreaterThan(8));
        }
    }
}