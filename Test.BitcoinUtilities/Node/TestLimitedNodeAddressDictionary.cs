using System;
using System.Collections.Generic;
using System.Net;
using BitcoinUtilities;
using BitcoinUtilities.Node;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Node
{
    [TestFixture]
    public class TestLimitedNodeAddressDictionary
    {
        [Test]
        public void TestMaxCapacity()
        {
            LimitedNodeAddressDictionary dict = new LimitedNodeAddressDictionary(3, TimeSpan.FromMinutes(10));

            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.1"), 8001));
            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.2"), 8002));
            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.3"), 8003));
            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.4"), 8004));

            Assert.That(dict.GetOldest(2), Is.EqualTo(new List<NodeAddress>
            {
                new NodeAddress(IPAddress.Parse("192.168.0.2"), 8002),
                new NodeAddress(IPAddress.Parse("192.168.0.3"), 8003)
            }));

            Assert.That(dict.GetNewest(2), Is.EqualTo(new List<NodeAddress>
            {
                new NodeAddress(IPAddress.Parse("192.168.0.4"), 8004),
                new NodeAddress(IPAddress.Parse("192.168.0.3"), 8003)
            }));

            Assert.That(dict.GetOldest(5), Is.EqualTo(new List<NodeAddress>
            {
                new NodeAddress(IPAddress.Parse("192.168.0.2"), 8002),
                new NodeAddress(IPAddress.Parse("192.168.0.3"), 8003),
                new NodeAddress(IPAddress.Parse("192.168.0.4"), 8004)
            }));

            Assert.That(dict.GetNewest(5), Is.EqualTo(new List<NodeAddress>
            {
                new NodeAddress(IPAddress.Parse("192.168.0.4"), 8004),
                new NodeAddress(IPAddress.Parse("192.168.0.3"), 8003),
                new NodeAddress(IPAddress.Parse("192.168.0.2"), 8002)
            }));

            Assert.False(dict.ContainsKey(new NodeAddress(IPAddress.Parse("192.168.0.1"), 8001)));
            Assert.True(dict.ContainsKey(new NodeAddress(IPAddress.Parse("192.168.0.2"), 8002)));
            Assert.True(dict.ContainsKey(new NodeAddress(IPAddress.Parse("192.168.0.3"), 8003)));
            Assert.True(dict.ContainsKey(new NodeAddress(IPAddress.Parse("192.168.0.4"), 8004)));
            Assert.False(dict.ContainsKey(new NodeAddress(IPAddress.Parse("192.168.0.5"), 8005)));
        }

        [Test]
        public void TestUpdateAndRemove()
        {
            LimitedNodeAddressDictionary dict = new LimitedNodeAddressDictionary(3, TimeSpan.FromMinutes(10));

            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.1"), 8001));
            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.2"), 8002));
            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.1"), 8001));
            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.3"), 8003));
            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.4"), 8004));

            Assert.That(dict.GetOldest(5), Is.EqualTo(new List<NodeAddress>
            {
                new NodeAddress(IPAddress.Parse("192.168.0.1"), 8001),
                new NodeAddress(IPAddress.Parse("192.168.0.3"), 8003),
                new NodeAddress(IPAddress.Parse("192.168.0.4"), 8004)
            }));

            Assert.False(dict.Remove(new NodeAddress(IPAddress.Parse("192.168.0.2"), 8002)));
            Assert.True(dict.Remove(new NodeAddress(IPAddress.Parse("192.168.0.3"), 8003)));

            Assert.That(dict.GetOldest(5), Is.EqualTo(new List<NodeAddress>
            {
                new NodeAddress(IPAddress.Parse("192.168.0.1"), 8001),
                new NodeAddress(IPAddress.Parse("192.168.0.4"), 8004)
            }));
        }

        [Test]
        public void TestTimeout()
        {
            LimitedNodeAddressDictionary dict = new LimitedNodeAddressDictionary(3, TimeSpan.FromMinutes(20));

            DateTime baseTime = DateTime.UtcNow;
            SystemTime.Override(baseTime);

            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.1"), 8333));
            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.2"), 8333));

            SystemTime.Override(baseTime.AddMinutes(5));

            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.3"), 8333));
            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.4"), 8333));

            SystemTime.Override(baseTime.AddMinutes(10));

            Assert.That(dict.GetOldest(5), Is.EqualTo(new List<NodeAddress>
            {
                new NodeAddress(IPAddress.Parse("192.168.0.2"), 8333),
                new NodeAddress(IPAddress.Parse("192.168.0.3"), 8333),
                new NodeAddress(IPAddress.Parse("192.168.0.4"), 8333)
            }));

            SystemTime.Override(baseTime.AddMinutes(22));

            Assert.That(dict.GetOldest(5), Is.EqualTo(new List<NodeAddress>
            {
                new NodeAddress(IPAddress.Parse("192.168.0.3"), 8333),
                new NodeAddress(IPAddress.Parse("192.168.0.4"), 8333)
            }));

            SystemTime.Override(baseTime.AddMinutes(27));

            Assert.That(dict.GetOldest(5), Is.EqualTo(new List<NodeAddress>()));
        }

        [Test]
        public void TestTimeInFuture()
        {
            LimitedNodeAddressDictionary dict = new LimitedNodeAddressDictionary(3, TimeSpan.FromMinutes(20));

            DateTime baseTime = DateTime.UtcNow;
            SystemTime.Override(baseTime);

            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.1"), 8333));
            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.2"), 8333));

            SystemTime.Override(baseTime.AddMinutes(10));

            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.3"), 8333));

            SystemTime.Override(baseTime.AddMinutes(5));

            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.4"), 8333));

            SystemTime.Override(baseTime.AddMinutes(10));

            Assert.That(dict.GetOldest(5), Is.EqualTo(new List<NodeAddress>
            {
                new NodeAddress(IPAddress.Parse("192.168.0.2"), 8333),
                new NodeAddress(IPAddress.Parse("192.168.0.3"), 8333),
                new NodeAddress(IPAddress.Parse("192.168.0.4"), 8333)
            }));

            SystemTime.Override(baseTime.AddMinutes(22));

            Assert.That(dict.GetOldest(5), Is.EqualTo(new List<NodeAddress>
            {
                new NodeAddress(IPAddress.Parse("192.168.0.3"), 8333),
                new NodeAddress(IPAddress.Parse("192.168.0.4"), 8333)
            }));

            SystemTime.Override(baseTime.AddMinutes(27));

            Assert.That(dict.GetOldest(5), Is.EqualTo(new List<NodeAddress>()));
        }

        [Test]
        public void TestGetNegativeCount()
        {
            LimitedNodeAddressDictionary dict = new LimitedNodeAddressDictionary(3, TimeSpan.FromMinutes(10));

            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.1"), 8001));
            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.2"), 8002));
            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.3"), 8003));
            dict.Add(new NodeAddress(IPAddress.Parse("192.168.0.4"), 8004));

            Assert.That(dict.GetOldest(-1), Is.EqualTo(new List<NodeAddress>()));
            Assert.That(dict.GetNewest(-1), Is.EqualTo(new List<NodeAddress>()));
        }
    }
}