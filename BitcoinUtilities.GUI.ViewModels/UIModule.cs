using System.Collections.Generic;
using System.Threading;
using BitcoinUtilities.GUI.Models;
using BitcoinUtilities.Node;
using BitcoinUtilities.Node.Components;
using BitcoinUtilities.P2P;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.GUI.ViewModels
{
    public class UIModule : INodeModule
    {
        private readonly ApplicationContext applicationContext;
        private readonly IViewContext viewContext;
        private readonly BitcoinNodeViewModel viewModel;
        private readonly string nodeStateChangedEventType;

        public UIModule(ApplicationContext applicationContext, IViewContext viewContext, BitcoinNodeViewModel viewModel, string nodeStateChangedEventType)
        {
            this.applicationContext = applicationContext;
            this.viewContext = viewContext;
            this.viewModel = viewModel;
            this.nodeStateChangedEventType = nodeStateChangedEventType;
        }

        public void CreateResources(BitcoinNode node, string dataFolder)
        {
        }

        public IReadOnlyCollection<IEventHandlingService> CreateNodeServices(BitcoinNode node, CancellationToken cancellationToken)
        {
            return new IEventHandlingService[] {new UIUpdaterService(applicationContext, viewContext, viewModel, nodeStateChangedEventType)};
        }

        public IReadOnlyCollection<IEventHandlingService> CreateEndpointServices(BitcoinNode node, BitcoinEndpoint endpoint)
        {
            return new IEventHandlingService[0];
        }
    }
}