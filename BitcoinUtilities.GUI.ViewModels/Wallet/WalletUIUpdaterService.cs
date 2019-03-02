using System.Threading;
using BitcoinUtilities.Node.Modules.Wallet.Events;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.GUI.ViewModels.Wallet
{
    public class WalletUIUpdaterService : EventHandlingService
    {
        private readonly Node.Modules.Wallet.Wallet wallet;
        private readonly WalletViewModel viewModel;
        private readonly IEventDispatcher eventDispatcher;
        private readonly IViewContext viewContext;

        private bool updateQueued;

        public WalletUIUpdaterService(Node.Modules.Wallet.Wallet wallet, WalletViewModel viewModel, IEventDispatcher eventDispatcher, IViewContext viewContext)
        {
            this.wallet = wallet;
            this.viewModel = viewModel;
            this.eventDispatcher = eventDispatcher;
            this.viewContext = viewContext;
            On<WalletBalanceChangedEvent>(QueueUpdate);
            On<WalletUIUpdateRequestedEvent>(UpdateViewModel);
        }

        private void QueueUpdate(WalletBalanceChangedEvent evt)
        {
            if (!updateQueued)
            {
                eventDispatcher.Raise(new WalletUIUpdateRequestedEvent());
                updateQueued = true;
            }
        }

        private void UpdateViewModel(WalletUIUpdateRequestedEvent evt)
        {
            // todo: use public keys instead?
            var addresses = wallet.GetAddresses();
            var outputs = wallet.GetAllOutputs();

            viewContext.Invoke(() => viewModel.Update(wallet.NetworkParameters, addresses, outputs));

            updateQueued = false;
            // avoiding to frequent updates of UI 
            Thread.Sleep(350);
        }

        class WalletUIUpdateRequestedEvent
        {
        }
    }
}