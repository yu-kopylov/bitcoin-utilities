using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace BitcoinUtilities.P2P
{
    public static class DnsSeeds
    {
        // source: https://github.com/bitcoinxt/bitcoinxt/blob/0.11E/src/chainparams.cpp
        private static readonly string[] seeds = new string[]
        {
            "seed.bitcoin.sipa.be", // Pieter Wiulle
            "dnsseed.bluematt.me", // Matt Corallo
            "dnsseed.bitcoin.dashjr.org", // Luke Dashjr
            "seed.bitcoinstats.com", // Christian Decker
            "seed.bitnodes.io", // Addy Yeow
            "dnsseed.vinumeris.com", // Mike Hearn
            "seed.bitcoin.jonasschnelli.ch" // Jonas Schnelli
        };

        /// <summary>
        /// Queries DNS seeds for nodes' addresses.
        /// </summary>
        /// <returns>List of nodes' addresses returned by the seeds.</returns>
        public static List<IPAddress> GetNodeAddresses()
        {
            //todo: differentiate between Main and Test networks
            HashSet<IPAddress> res = new HashSet<IPAddress>();

            foreach (string dnsSeed in seeds)
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