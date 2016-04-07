using System.Net;
using BitcoinUtilities.P2P.Messages;

namespace BitcoinUtilities.P2P
{
    public class BitcoinPeerInfo
    {
        public BitcoinPeerInfo(IPEndPoint ipEndpoint, VersionMessage versionMessage)
        {
            IpEndpoint = ipEndpoint;
            VersionMessage = versionMessage;
        }

        public IPEndPoint IpEndpoint { get; private set; }
        public VersionMessage VersionMessage { get; private set; }
    }
}