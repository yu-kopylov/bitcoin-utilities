namespace BitcoinUtilities.GUI.ViewModels.Wallet
{
    public class AddWalletAddressViewModel
    {
        private readonly IViewContext viewContext;
        private readonly BitcoinNetworkKind networkKind;
        private readonly WalletViewModel wallet;

        public AddWalletAddressViewModel(IViewContext viewContext, BitcoinNetworkKind networkKind, WalletViewModel wallet)
        {
            this.viewContext = viewContext;
            this.networkKind = networkKind;
            this.wallet = wallet;
        }

        public string Wif { get; set; }

        public bool AddAddress()
        {
            // todo: use network parameters
            if (!BitcoinUtilities.Wif.TryDecode(networkKind, Wif, out var privateKey, out bool _))
            {
                viewContext.ShowError("Invalid WIF encoding.");
                return false;
            }

            wallet.AddPrivateKey(privateKey);
            return true;
        }
    }
}