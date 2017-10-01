using System.Collections.Generic;
using System.Net;
using BitcoinUtilities;
using BitcoinUtilities.P2P;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    public class TestDnsSeeds
    {
        [Test]
        public void TestBitcoinCoreMain()
        {
            List<IPAddress> addresses = DnsSeeds.GetNodeAddresses(NetworkParameters.BitcoinCoreMain.GetDnsSeeds());
            Assert.That(addresses.Count, Is.GreaterThan(8));
        }

        [Test]
        public void TestBitcoinCashMain()
        {
            List<IPAddress> addresses = DnsSeeds.GetNodeAddresses(NetworkParameters.BitcoinCashMain.GetDnsSeeds());
            Assert.That(addresses.Count, Is.GreaterThan(8));
        }
    }
}