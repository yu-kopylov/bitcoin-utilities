using BitcoinUtilities.GUI.Models;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.GUI.ViewModels
{
    public class UIUpdaterService : EventHandlingService
    {
        public UIUpdaterService(IViewContext viewContext, BitcoinNodeViewModel viewModel, ApplicationContext applicationContext, string nodeStateChangedEventType)
        {
            // todo: this service is prone to deadlocks on shutdown: (uiThread: "stop button" -> serviceThread.Join; serviceThread: event -> ui.invoke)
            On<NodeConnectionsChangedEvent>(e => applicationContext.EventManager.Notify(nodeStateChangedEventType));
            On<BestHeadChangedEvent>(e => applicationContext.EventManager.Notify(nodeStateChangedEventType));
            On<UtxoChangedEvent>(e => viewContext.Invoke(() => viewModel.BestChainHeight = e.LastHeaderHeight));
        }
    }
}