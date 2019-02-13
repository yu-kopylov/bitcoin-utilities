using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BitcoinUtilities.Node.Components;
using BitcoinUtilities.P2P;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Modules.Outputs
{
    public class UtxoModule : INodeModule
    {
        public void CreateResources(BitcoinNode node, string dataFolder)
        {
            UtxoStorage storage = null;
            UtxoRepository repository = null;
            try
            {
                storage = UtxoStorage.Open(Path.Combine(dataFolder, "utxo.db"));
                repository = new UtxoRepository(storage, node.EventDispatcher);
                node.Resources.Add(repository);
            }
            catch (Exception)
            {
                repository?.Dispose();
                storage?.Dispose();
                throw;
            }
        }

        public IReadOnlyCollection<IEventHandlingService> CreateNodeServices(BitcoinNode node, CancellationToken cancellationToken)
        {
            return new IEventHandlingService[]
            {
                new UtxoUpdateService(node.EventDispatcher, node.Blockchain, node.Resources.Get<UtxoRepository>()),
                new SignatureValidationService(node.EventDispatcher)
            };
        }

        public IReadOnlyCollection<IEventHandlingService> CreateEndpointServices(BitcoinNode node, BitcoinEndpoint endpoint)
        {
            return new IEventHandlingService[0];
        }
    }
}