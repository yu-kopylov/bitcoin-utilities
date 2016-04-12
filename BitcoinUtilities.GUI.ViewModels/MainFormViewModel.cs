using BitcoinUtilities.GUI.Models;

namespace BitcoinUtilities.GUI.ViewModels
{
    public class MainFormViewModel
    {
        private readonly ApplicationContext applicationContext;
        private readonly IViewContext viewContext;

        public MainFormViewModel(ApplicationContext applicationContext, IViewContext viewContext)
        {
            this.applicationContext = applicationContext;
            this.viewContext = viewContext;

            BitcoinNode = new BitcoinNodeViewModel(applicationContext, viewContext);
            PaperWallet = new PaperWalletViewModel(viewContext);
        }

        public BitcoinNodeViewModel BitcoinNode { get; set; }
        public PaperWalletViewModel PaperWallet { get; set; }
    }
}