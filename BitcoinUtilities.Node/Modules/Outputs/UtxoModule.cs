using System.Collections.Generic;
using System.Threading;
using BitcoinUtilities.Node.Components;
using BitcoinUtilities.P2P;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Modules.Outputs
{
    public class UtxoModule : INodeModule
    {
        public void CreateResources(BitcoinNode node)
        {
        }

        public IReadOnlyCollection<IEventHandlingService> CreateNodeServices(BitcoinNode node, CancellationToken cancellationToken)
        {
            return new IEventHandlingService[]
            {
                new UtxoUpdateService(node.EventServiceController, node.Blockchain, node.UtxoStorage),
                new SignatureValidationService(node.EventServiceController)
            };
        }

        public IReadOnlyCollection<IEventHandlingService> CreateEndpointServices(BitcoinNode node, BitcoinEndpoint endpoint)
        {
            return new IEventHandlingService[0];
        }
    }
}