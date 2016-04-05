using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using BitcoinUtilities.P2P;

namespace BitcoinUtilities.Node
{
    public class NodeDiscoveryThread
    {
        private static readonly TimeSpan stateRefreshInterval = TimeSpan.FromMilliseconds(5000);
        private static readonly TimeSpan dnsSeedsRefreshInterval = TimeSpan.FromMinutes(60);
        private const int TargetEnpointsCount = 8;

        private readonly BitcoinNode node;
        private readonly CancellationToken cancellationToken;

        private readonly AutoResetEvent signalEvent = new AutoResetEvent(true);
        private volatile bool started;

        private DateTime lastDnsSeedsLookup = DateTime.MinValue;
        private readonly HashSet<IPAddress> knownAddresses = new HashSet<IPAddress>();

        public NodeDiscoveryThread(BitcoinNode node, CancellationToken cancellationToken)
        {
            this.node = node;

            cancellationToken.Register(Signal);
            this.cancellationToken = cancellationToken;
        }

        public void Run()
        {
            if (started)
            {
                throw new InvalidOperationException($"{nameof(NodeDiscoveryThread)} was already started once.");
            }

            started = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                CheckDnsSeeds();
                ConnectToNodes();
                signalEvent.WaitOne(stateRefreshInterval);
            }
        }

        /// <summary>
        /// Signals thread to stop waiting and process changed state.
        /// </summary>
        public void Signal()
        {
            signalEvent.Set();
        }

        private void ConnectToNodes()
        {
            //todo: count only outgoing endpoints
            if (node.Endpoints.Count >= TargetEnpointsCount)
            {
                return;
            }

            List<IPAddress> knownAddressesList = knownAddresses.ToList();
            Random random = new Random();

            //todo: avoid infinite loop if we all knownAddresses are either unreachable or already connected
            //todo: count only outgoing endpoints
            //todo: connect only to full nodes and check protocol version
            while (!cancellationToken.IsCancellationRequested && node.Endpoints.Count < TargetEnpointsCount && knownAddressesList.Any())
            {
                //todo: don't connect to same node twice
                IPAddress selectedAddress = knownAddressesList[random.Next(knownAddressesList.Count)];
                node.ConnectTo(selectedAddress.ToString(), 8333);
            }
        }

        private void CheckDnsSeeds()
        {
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
            lastDnsSeedsLookup = now;

            List<IPAddress> addresses = DnsSeeds.GetNodeAddresses();
            foreach (IPAddress address in addresses)
            {
                //todo: periodically remove addresses from knownAddresses
                //todo: add persistent collection of known nodes
                knownAddresses.Add(address);
            }
        }
    }
}