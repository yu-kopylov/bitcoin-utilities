using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Collections;

namespace BitcoinUtilities.Node
{
    internal class LimitedNodeAddressDictionary
    {
        private readonly int maxAddressCount;
        private readonly TimeSpan timeout;

        private readonly LinkedDictionary<NodeAddress, DateTime> addresses = new LinkedDictionary<NodeAddress, DateTime>();

        public LimitedNodeAddressDictionary(int maxAddressCount, TimeSpan timeout)
        {
            this.maxAddressCount = maxAddressCount;
            this.timeout = timeout;
        }

        public List<NodeAddress> GetOldest(int count)
        {
            Truncate(SystemTime.UtcNow);
            return addresses.Take(count).Select(p => p.Key).ToList();
        }

        public List<NodeAddress> GetNewest(int count)
        {
            Truncate(SystemTime.UtcNow);
            int skipCount = addresses.Count - count;
            List<NodeAddress> res = addresses.Skip(skipCount).Select(p => p.Key).ToList();
            res.Reverse();
            return res;
        }

        public bool ContainsKey(NodeAddress address)
        {
            Truncate(SystemTime.UtcNow);
            return addresses.ContainsKey(address);
        }

        public void Add(NodeAddress address)
        {
            DateTime now = SystemTime.UtcNow;

            addresses.Remove(address);

            FixFutureDates(now);

            addresses.Add(address, now);

            Truncate(now);
        }

        public bool Remove(NodeAddress address)
        {
            return addresses.Remove(address);
        }

        private void FixFutureDates(DateTime now)
        {
            if (addresses.Count == 0)
            {
                return;
            }

            KeyValuePair<NodeAddress, DateTime> lastAddress = addresses.GetLast();
            if (lastAddress.Value <= now)
            {
                return;
            }

            // this should be a very rare event

            foreach (KeyValuePair<NodeAddress, DateTime> pair in addresses)
            {
                if (pair.Value > now)
                {
                    addresses[pair.Key] = now;
                }
            }
        }

        private void Truncate(DateTime now)
        {
            DateTime threshold = now - timeout;
            int minAddressesToRemove = addresses.Count - maxAddressCount;

            List<NodeAddress> addressesToRemove = new List<NodeAddress>();
            foreach (KeyValuePair<NodeAddress, DateTime> pair in addresses)
            {
                if (addressesToRemove.Count < minAddressesToRemove || pair.Value < threshold)
                {
                    addressesToRemove.Add(pair.Key);
                }
                else
                {
                    break;
                }
            }

            foreach (NodeAddress address in addressesToRemove)
            {
                addresses.Remove(address);
            }
        }
    }
}