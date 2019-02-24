using System.Collections.Generic;
using System.Threading;
using BitcoinUtilities.Node.Components;
using BitcoinUtilities.P2P;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Modules.Wallet
{
    public class WalletModule : INodeModule
    {
        public void CreateResources(BitcoinNode node, string dataFolder)
        {
            node.Resources.Add(new Wallet(node.NetworkParameters, node.EventDispatcher));
        }

        public IReadOnlyCollection<IEventHandlingService> CreateNodeServices(BitcoinNode node, CancellationToken cancellationToken)
        {
            return new IEventHandlingService[] {new WalletUpdateService(node.Blockchain, node.Resources.Get<Wallet>(), node.EventDispatcher)};
        }

        public IReadOnlyCollection<IEventHandlingService> CreateEndpointServices(BitcoinNode node, BitcoinEndpoint endpoint)
        {
            return new IEventHandlingService[0];
        }
    }
}