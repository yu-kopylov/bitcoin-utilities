﻿using System.Collections.Generic;
using System.Threading;
using BitcoinUtilities.Node.Components;
using BitcoinUtilities.P2P;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Modules.Headers
{
    public class HeadersModule : INodeModule
    {
        public void CreateResources(BitcoinNode node, string dataFolder)
        {
        }

        public IReadOnlyCollection<IEventHandlingService> CreateNodeServices(BitcoinNode node, CancellationToken cancellationToken)
        {
            return new IEventHandlingService[0];
        }

        public IReadOnlyCollection<IEventHandlingService> CreateEndpointServices(BitcoinNode node, BitcoinEndpoint endpoint)
        {
            return new[] {new HeaderDownloadService(node.EventDispatcher, node.Blockchain, node.NetworkParameters, endpoint)};
        }
    }
}