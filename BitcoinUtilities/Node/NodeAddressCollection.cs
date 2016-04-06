using System;
using System.Collections.Generic;

namespace BitcoinUtilities.Node
{
    /// <summary>
    /// Thread-safe collection of known Bitcoin nodes' addresses.
    /// </summary>
    public class NodeAddressCollection
    {
        private const int MaxUntestedAddressesCount = 1000;
        private const int MaxRejectedAddressesCount = 1000;
        private const int MaxBannedAddressesCount = 1000;
        private const int MaxConfirmedAddressesCount = 32;
        private const int MaxTemporaryRejectedAddressesCount = 32;

        private static readonly TimeSpan untestedTimeout = TimeSpan.FromHours(12);
        private static readonly TimeSpan rejectedTimeout = TimeSpan.FromHours(1);
        private static readonly TimeSpan bannedTimeout = TimeSpan.FromHours(24);
        private static readonly TimeSpan confirmedTimeout = TimeSpan.FromDays(14);
        private static readonly TimeSpan temporaryRejectedTimeout = TimeSpan.FromDays(14);

        private readonly object lockObject = new object();

        private readonly LimitedNodeAddressDictionary untested = new LimitedNodeAddressDictionary(MaxUntestedAddressesCount, untestedTimeout);
        private readonly LimitedNodeAddressDictionary rejected = new LimitedNodeAddressDictionary(MaxRejectedAddressesCount, rejectedTimeout);
        private readonly LimitedNodeAddressDictionary banned = new LimitedNodeAddressDictionary(MaxBannedAddressesCount, bannedTimeout);
        private readonly LimitedNodeAddressDictionary confirmed = new LimitedNodeAddressDictionary(MaxConfirmedAddressesCount, confirmedTimeout);
        private readonly LimitedNodeAddressDictionary temporaryRejected = new LimitedNodeAddressDictionary(MaxTemporaryRejectedAddressesCount, temporaryRejectedTimeout);

        public List<NodeAddress> GetNewestUntested(int count)
        {
            lock (lockObject)
            {
                return untested.GetNewest(count);
            }
        }

        public List<NodeAddress> GetOldestRejected(int count)
        {
            lock (lockObject)
            {
                return rejected.GetOldest(count);
            }
        }

        public List<NodeAddress> GetNewestBanned(int count)
        {
            lock (lockObject)
            {
                return banned.GetNewest(count);
            }
        }

        public List<NodeAddress> GetNewestConfirmed(int count)
        {
            lock (lockObject)
            {
                return confirmed.GetNewest(count);
            }
        }

        public List<NodeAddress> GetOldestTemporaryRejected(int count)
        {
            lock (lockObject)
            {
                return temporaryRejected.GetOldest(count);
            }
        }

        public void Add(NodeAddress address)
        {
            lock (lockObject)
            {
                if (!rejected.ContainsKey(address) && !banned.ContainsKey(address) && !confirmed.ContainsKey(address) && !temporaryRejected.ContainsKey(address))
                {
                    untested.Add(address);
                }
            }
        }

        public void Confirm(NodeAddress address)
        {
            lock (lockObject)
            {
                if (banned.ContainsKey(address))
                {
                    return;
                }

                untested.Remove(address);
                rejected.Remove(address);
                temporaryRejected.Remove(address);

                confirmed.Add(address);
            }
        }

        public void Reject(NodeAddress address)
        {
            lock (lockObject)
            {
                if (confirmed.Remove(address) || temporaryRejected.Remove(address))
                {
                    temporaryRejected.Add(address);
                }
                else if (banned.ContainsKey(address))
                {
                    // do nothing 
                }
                else
                {
                    untested.Remove(address);
                    rejected.Remove(address);
                    rejected.Add(address);
                }
            }
        }

        public void Ban(NodeAddress address)
        {
            lock (lockObject)
            {
                untested.Remove(address);
                rejected.Remove(address);
                confirmed.Remove(address);
                temporaryRejected.Remove(address);

                banned.Add(address);
            }
        }
    }
}