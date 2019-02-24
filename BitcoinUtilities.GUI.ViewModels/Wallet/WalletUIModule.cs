using System.Collections.Generic;
using System.Threading;
using BitcoinUtilities.Node;
using BitcoinUtilities.Node.Components;
using BitcoinUtilities.P2P;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.GUI.ViewModels.Wallet
{
    public class WalletUIModule : INodeModule
    {
        private readonly IViewContext viewContext;
        private readonly WalletViewModel viewModel;

        public WalletUIModule(IViewContext viewContext, WalletViewModel viewModel)
        {
            this.viewContext = viewContext;
            this.viewModel = viewModel;
        }

        public void CreateResources(BitcoinNode node, string dataFolder)
        {
        }

        public IReadOnlyCollection<IEventHandlingService> CreateNodeServices(BitcoinNode node, CancellationToken cancellationToken)
        {
            return new IEventHandlingService[] {new WalletUIUpdaterService(node.Resources.Get<Node.Modules.Wallet.Wallet>(), viewModel, node.EventDispatcher, viewContext)};
        }

        public IReadOnlyCollection<IEventHandlingService> CreateEndpointServices(BitcoinNode node, BitcoinEndpoint endpoint)
        {
            return new IEventHandlingService[0];
        }
    }
}