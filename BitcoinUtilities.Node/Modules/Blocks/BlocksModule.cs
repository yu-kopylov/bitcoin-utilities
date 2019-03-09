using System.Collections.Generic;
using System.Threading;
using BitcoinUtilities.Node.Components;
using BitcoinUtilities.P2P;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Modules.Blocks
{
    public class BlocksModule : INodeModule
    {
        public void CreateResources(BitcoinNode node, string dataFolder)
        {
            node.Resources.Add(new BlockDownloadRequestRepository(node.EventDispatcher));
            node.Resources.Add(new BlockRepository());

            var genesisBlock = node.NetworkParameters.GenesisBlock;
            byte[] genesisBlockHash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(genesisBlock.BlockHeader.Write));
            node.Resources.Get<BlockRepository>().AddBlock(genesisBlockHash, genesisBlock);
        }

        public IReadOnlyCollection<IEventHandlingService> CreateNodeServices(BitcoinNode node, CancellationToken cancellationToken)
        {
            return new IEventHandlingService[]
            {
                new BlockRequestProcessor(node.EventDispatcher, node.Resources.Get<BlockRepository>(), node.Resources.Get<BlockDownloadRequestRepository>()),
            };
        }

        public IReadOnlyCollection<IEventHandlingService> CreateEndpointServices(BitcoinNode node, BitcoinEndpoint endpoint)
        {
            return new IEventHandlingService[]
            {
                new BlockDownloadService(node.NetworkParameters, node.EventDispatcher, node.Resources.Get<BlockDownloadRequestRepository>(), node.Resources.Get<BlockRepository>(), endpoint)
            };
        }
    }
}