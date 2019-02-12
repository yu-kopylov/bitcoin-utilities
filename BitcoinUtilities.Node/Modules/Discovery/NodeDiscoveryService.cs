using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.P2P;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Modules.Discovery
{
    public class NodeDiscoveryService : EventHandlingService
    {
        private static readonly TimeSpan dnsSeedsRefreshInterval = TimeSpan.FromMinutes(60);

        private readonly Random random = new Random();

        private DateTime lastDnsSeedsLookup = DateTime.MinValue;

        private readonly BitcoinNode node;
        private readonly CancellationToken cancellationToken;

        public NodeDiscoveryService(BitcoinNode node, CancellationToken cancellationToken)
        {
            this.node = node;
            this.cancellationToken = cancellationToken;

            On<NodeConnectionsChangedEvent>(e => CheckState());
        }

        public override void OnStart()
        {
            base.OnStart();
            CheckState();
        }

        private void CheckState()
        {
            CheckDnsSeeds();
            ConnectToNodes();
        }

        private void ConnectToNodes()
        {
            if (cancellationToken.IsCancellationRequested || IsMaximumConnectionCountReached())
            {
                return;
            }

            List<NodeAddress> addresses = SelectNodesToConnect();

            //todo: limit number of concurrent connection attempts?
            Parallel.ForEach(addresses, address =>
            {
                if (cancellationToken.IsCancellationRequested || IsMaximumConnectionCountReached())
                {
                    return;
                }

                if (node.ConnectionCollection.IsConnected(address.Address))
                {
                    return;
                }

                //todo: don't connect to already connected nodes
                node.ConnectTo(address);
            });
        }

        private bool IsMaximumConnectionCountReached()
        {
            return node.ConnectionCollection.OutgoingConnectionsCount >= node.ConnectionCollection.MaxOutgoingConnectionsCount;
        }

        private List<NodeAddress> SelectNodesToConnect()
        {
            const int maxAddressCount = 20;
            List<NodeAddress> res = new List<NodeAddress>();
            res.AddRange(node.AddressCollection.GetNewestConfirmed(2));
            res.AddRange(node.AddressCollection.GetOldestTemporaryRejected(2));
            res.AddRange(node.AddressCollection.GetNewestUntested(maxAddressCount - res.Count));
            res.AddRange(node.AddressCollection.GetOldestRejected(maxAddressCount - res.Count));
            return res;
        }

        private void CheckDnsSeeds()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            if (lastDnsSeedsLookup > now)
            {
                lastDnsSeedsLookup = now;
                return;
            }

            if (lastDnsSeedsLookup.Add(dnsSeedsRefreshInterval) >= now)
            {
                return;
            }

            //todo: if network was inaccessible on start of application seeds will not be received for up to dnsSeedsRefreshInterval (1 hour)
            lastDnsSeedsLookup = now;

            List<IPAddress> addresses = DnsSeeds.GetNodeAddresses(node.NetworkParameters.GetDnsSeeds());

            addresses = Suffle(addresses);

            foreach (IPAddress address in addresses)
            {
                //todo: persist collection of known nodes
                node.AddressCollection.Add(new NodeAddress(address, 8333));
            }
        }

        private List<IPAddress> Suffle(List<IPAddress> addresses)
        {
            List<IPAddress> source = new List<IPAddress>(addresses);
            List<IPAddress> res = new List<IPAddress>(addresses.Count);

            while (source.Count > 0)
            {
                int idx = random.Next(source.Count);
                res.Add(source[idx]);

                int lastIdx = source.Count - 1;
                source[idx] = source[lastIdx];
                source.RemoveAt(lastIdx);
            }

            return res;
        }
    }
}