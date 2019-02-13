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
            node.Resources.Add(new BlockRequestCollection());
            node.Resources.Add(new BlockRepository(node.EventDispatcher, node.Resources.Get<BlockRequestCollection>()));

            var genesisBlock = node.NetworkParameters.GenesisBlock;
            byte[] genesisBlockHash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(genesisBlock.BlockHeader.Write));
            node.Resources.Get<BlockRepository>().AddBlock(genesisBlockHash, genesisBlock);
        }

        public IReadOnlyCollection<IEventHandlingService> CreateNodeServices(BitcoinNode node, CancellationToken cancellationToken)
        {
            return new IEventHandlingService[] {node.Resources.Get<BlockRepository>()};
        }

        public IReadOnlyCollection<IEventHandlingService> CreateEndpointServices(BitcoinNode node, BitcoinEndpoint endpoint)
        {
            return new IEventHandlingService[]
            {
                new BlockDownloadService(node.Resources.Get<BlockRequestCollection>(), node.Resources.Get<BlockRepository>(), endpoint)
            };
        }
    }
}