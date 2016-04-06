using System.Collections.Generic;
using System.Net;
using BitcoinUtilities.Node;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Node
{
    [TestFixture]
    public class TestNodeAddressCollection
    {
        [Test]
        public void TestUnknownAddress()
        {
            NodeAddressCollection addressCollection = new NodeAddressCollection();

            NodeAddress address1 = new NodeAddress(IPAddress.Parse("192.168.0.1"), 8333);
            NodeAddress address2 = new NodeAddress(IPAddress.Parse("192.168.0.2"), 8333);
            NodeAddress address3 = new NodeAddress(IPAddress.Parse("192.168.0.3"), 8333);
            NodeAddress address4 = new NodeAddress(IPAddress.Parse("192.168.0.4"), 8333);

            addressCollection.Add(address1);
            addressCollection.Reject(address2);
            addressCollection.Ban(address3);
            addressCollection.Confirm(address4);

            Assert.That(addressCollection.GetNewestUntested(10), Is.EqualTo(new List<NodeAddress> {address1}));
            Assert.That(addressCollection.GetOldestRejected(10), Is.EqualTo(new List<NodeAddress> {address2}));
            Assert.That(addressCollection.GetNewestBanned(10), Is.EqualTo(new List<NodeAddress> {address3}));
            Assert.That(addressCollection.GetNewestConfirmed(10), Is.EqualTo(new List<NodeAddress> {address4}));
            Assert.That(addressCollection.GetOldestTemporaryRejected(10), Is.EqualTo(new List<NodeAddress>()));
        }

        [Test]
        public void TestUntestedTransitions()
        {
            NodeAddressCollection addressCollection = new NodeAddressCollection();

            NodeAddress address0 = new NodeAddress(IPAddress.Parse("192.168.0.9"), 8333);
            NodeAddress address1 = new NodeAddress(IPAddress.Parse("192.168.0.1"), 8333);
            NodeAddress address2 = new NodeAddress(IPAddress.Parse("192.168.0.2"), 8333);
            NodeAddress address3 = new NodeAddress(IPAddress.Parse("192.168.0.3"), 8333);
            NodeAddress address4 = new NodeAddress(IPAddress.Parse("192.168.0.4"), 8333);

            addressCollection.Add(address0);
            addressCollection.Add(address1);
            addressCollection.Add(address2);
            addressCollection.Add(address3);
            addressCollection.Add(address4);

            Assert.That(addressCollection.GetNewestUntested(10), Is.EqualTo(new List<NodeAddress> {address4, address3, address2, address1, address0}));

            addressCollection.Add(address0);
            addressCollection.Reject(address2);
            addressCollection.Ban(address3);
            addressCollection.Confirm(address4);

            Assert.That(addressCollection.GetNewestUntested(10), Is.EqualTo(new List<NodeAddress> {address0, address1}));
            Assert.That(addressCollection.GetOldestRejected(10), Is.EqualTo(new List<NodeAddress> {address2}));
            Assert.That(addressCollection.GetNewestBanned(10), Is.EqualTo(new List<NodeAddress> {address3}));
            Assert.That(addressCollection.GetNewestConfirmed(10), Is.EqualTo(new List<NodeAddress> {address4}));
            Assert.That(addressCollection.GetOldestTemporaryRejected(10), Is.EqualTo(new List<NodeAddress>()));
        }

        [Test]
        public void TestRejectedTransitions()
        {
            NodeAddressCollection addressCollection = new NodeAddressCollection();

            NodeAddress address0 = new NodeAddress(IPAddress.Parse("192.168.0.9"), 8333);
            NodeAddress address1 = new NodeAddress(IPAddress.Parse("192.168.0.1"), 8333);
            NodeAddress address2 = new NodeAddress(IPAddress.Parse("192.168.0.2"), 8333);
            NodeAddress address3 = new NodeAddress(IPAddress.Parse("192.168.0.3"), 8333);
            NodeAddress address4 = new NodeAddress(IPAddress.Parse("192.168.0.4"), 8333);

            addressCollection.Add(address0);
            addressCollection.Add(address1);
            addressCollection.Add(address2);
            addressCollection.Add(address3);
            addressCollection.Add(address4);

            addressCollection.Reject(address0);
            addressCollection.Reject(address1);
            addressCollection.Reject(address2);
            addressCollection.Reject(address3);
            addressCollection.Reject(address4);

            Assert.That(addressCollection.GetOldestRejected(10), Is.EqualTo(new List<NodeAddress> {address0, address1, address2, address3, address4}));

            addressCollection.Add(address2);
            addressCollection.Reject(address1);
            addressCollection.Ban(address3);
            addressCollection.Confirm(address4);

            Assert.That(addressCollection.GetNewestUntested(10), Is.EqualTo(new List<NodeAddress>()));
            Assert.That(addressCollection.GetOldestRejected(10), Is.EqualTo(new List<NodeAddress> {address0, address2, address1}));
            Assert.That(addressCollection.GetNewestBanned(10), Is.EqualTo(new List<NodeAddress> {address3}));
            Assert.That(addressCollection.GetNewestConfirmed(10), Is.EqualTo(new List<NodeAddress> {address4}));
            Assert.That(addressCollection.GetOldestTemporaryRejected(10), Is.EqualTo(new List<NodeAddress>()));
        }

        [Test]
        public void TestBannedTransitions()
        {
            NodeAddressCollection addressCollection = new NodeAddressCollection();

            NodeAddress address0 = new NodeAddress(IPAddress.Parse("192.168.0.9"), 8333);
            NodeAddress address1 = new NodeAddress(IPAddress.Parse("192.168.0.1"), 8333);
            NodeAddress address2 = new NodeAddress(IPAddress.Parse("192.168.0.2"), 8333);
            NodeAddress address3 = new NodeAddress(IPAddress.Parse("192.168.0.3"), 8333);
            NodeAddress address4 = new NodeAddress(IPAddress.Parse("192.168.0.4"), 8333);

            addressCollection.Add(address0);
            addressCollection.Add(address1);
            addressCollection.Add(address2);
            addressCollection.Add(address3);
            addressCollection.Add(address4);

            addressCollection.Ban(address0);
            addressCollection.Ban(address1);
            addressCollection.Ban(address2);
            addressCollection.Ban(address3);
            addressCollection.Ban(address4);

            Assert.That(addressCollection.GetNewestBanned(10), Is.EqualTo(new List<NodeAddress> {address4, address3, address2, address1, address0}));

            addressCollection.Add(address1);
            addressCollection.Reject(address2);
            addressCollection.Ban(address3);
            addressCollection.Confirm(address4);

            Assert.That(addressCollection.GetNewestUntested(10), Is.EqualTo(new List<NodeAddress>()));
            Assert.That(addressCollection.GetOldestRejected(10), Is.EqualTo(new List<NodeAddress>()));
            Assert.That(addressCollection.GetNewestBanned(10), Is.EqualTo(new List<NodeAddress> {address3, address4, address2, address1, address0}));
            Assert.That(addressCollection.GetNewestConfirmed(10), Is.EqualTo(new List<NodeAddress>()));
            Assert.That(addressCollection.GetOldestTemporaryRejected(10), Is.EqualTo(new List<NodeAddress>()));
        }

        [Test]
        public void TestConfirmedTransitions()
        {
            NodeAddressCollection addressCollection = new NodeAddressCollection();

            NodeAddress address0 = new NodeAddress(IPAddress.Parse("192.168.0.9"), 8333);
            NodeAddress address1 = new NodeAddress(IPAddress.Parse("192.168.0.1"), 8333);
            NodeAddress address2 = new NodeAddress(IPAddress.Parse("192.168.0.2"), 8333);
            NodeAddress address3 = new NodeAddress(IPAddress.Parse("192.168.0.3"), 8333);
            NodeAddress address4 = new NodeAddress(IPAddress.Parse("192.168.0.4"), 8333);

            addressCollection.Add(address0);
            addressCollection.Add(address1);
            addressCollection.Add(address2);
            addressCollection.Add(address3);
            addressCollection.Add(address4);

            addressCollection.Confirm(address0);
            addressCollection.Confirm(address1);
            addressCollection.Confirm(address2);
            addressCollection.Confirm(address3);
            addressCollection.Confirm(address4);

            Assert.That(addressCollection.GetNewestConfirmed(10), Is.EqualTo(new List<NodeAddress> {address4, address3, address2, address1, address0}));

            addressCollection.Add(address1);
            addressCollection.Reject(address2);
            addressCollection.Ban(address3);
            addressCollection.Confirm(address0);

            Assert.That(addressCollection.GetNewestUntested(10), Is.EqualTo(new List<NodeAddress>()));
            Assert.That(addressCollection.GetOldestRejected(10), Is.EqualTo(new List<NodeAddress>()));
            Assert.That(addressCollection.GetNewestBanned(10), Is.EqualTo(new List<NodeAddress> {address3}));
            Assert.That(addressCollection.GetNewestConfirmed(10), Is.EqualTo(new List<NodeAddress> {address0, address4, address1}));
            Assert.That(addressCollection.GetOldestTemporaryRejected(10), Is.EqualTo(new List<NodeAddress> {address2}));
        }

        [Test]
        public void TestTemporaryRejectedTransitions()
        {
            NodeAddressCollection addressCollection = new NodeAddressCollection();

            NodeAddress address0 = new NodeAddress(IPAddress.Parse("192.168.0.9"), 8333);
            NodeAddress address1 = new NodeAddress(IPAddress.Parse("192.168.0.1"), 8333);
            NodeAddress address2 = new NodeAddress(IPAddress.Parse("192.168.0.2"), 8333);
            NodeAddress address3 = new NodeAddress(IPAddress.Parse("192.168.0.3"), 8333);
            NodeAddress address4 = new NodeAddress(IPAddress.Parse("192.168.0.4"), 8333);

            addressCollection.Add(address0);
            addressCollection.Add(address1);
            addressCollection.Add(address2);
            addressCollection.Add(address3);
            addressCollection.Add(address4);

            addressCollection.Confirm(address0);
            addressCollection.Confirm(address1);
            addressCollection.Confirm(address2);
            addressCollection.Confirm(address3);
            addressCollection.Confirm(address4);

            addressCollection.Reject(address0);
            addressCollection.Reject(address1);
            addressCollection.Reject(address2);
            addressCollection.Reject(address3);
            addressCollection.Reject(address4);

            Assert.That(addressCollection.GetOldestTemporaryRejected(10), Is.EqualTo(new List<NodeAddress> {address0, address1, address2, address3, address4}));

            addressCollection.Add(address1);
            addressCollection.Reject(address0);
            addressCollection.Ban(address3);
            addressCollection.Confirm(address4);

            Assert.That(addressCollection.GetNewestUntested(10), Is.EqualTo(new List<NodeAddress>()));
            Assert.That(addressCollection.GetOldestRejected(10), Is.EqualTo(new List<NodeAddress>()));
            Assert.That(addressCollection.GetNewestBanned(10), Is.EqualTo(new List<NodeAddress> {address3}));
            Assert.That(addressCollection.GetNewestConfirmed(10), Is.EqualTo(new List<NodeAddress> {address4}));
            Assert.That(addressCollection.GetOldestTemporaryRejected(10), Is.EqualTo(new List<NodeAddress> {address1, address2, address0}));
        }
    }
}