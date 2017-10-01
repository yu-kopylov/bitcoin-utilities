using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace BitcoinUtilities.P2P
{
    public static class DnsSeeds
    {
        /// <summary>
        /// Queries the given DNS seeds for IP addresses of known nodes.
        /// </summary>
        /// <param name="dnsSeeds">The list of DNS seeds.</param>
        /// <returns>List of IP addresses returned by the seeds.</returns>
        public static List<IPAddress> GetNodeAddresses(string[] dnsSeeds)
        {
            HashSet<IPAddress> res = new HashSet<IPAddress>();

            foreach (string dnsSeed in dnsSeeds)
            {
                var addresses = QueryDns(dnsSeed);
                foreach (IPAddress address in addresses)
                {
                    res.Add(address);
                }
            }

            return res.ToList();
        }

        private static IPAddress[] QueryDns(string dnsSeed)
        {
            try
            {
                return Dns.GetHostAddresses(dnsSeed);
            }
            catch (SocketException)
            {
            }
            return new IPAddress[0];
        }
    }
}