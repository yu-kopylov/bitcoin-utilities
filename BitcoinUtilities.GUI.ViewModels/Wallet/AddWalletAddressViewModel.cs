namespace BitcoinUtilities.GUI.ViewModels.Wallet
{
    public class AddWalletAddressViewModel
    {
        private readonly IViewContext viewContext;
        private readonly NetworkParameters networkParameters;
        private readonly WalletViewModel wallet;

        public AddWalletAddressViewModel(IViewContext viewContext, NetworkParameters networkParameters, WalletViewModel wallet)
        {
            this.viewContext = viewContext;
            this.networkParameters = networkParameters;
            this.wallet = wallet;
        }

        public string Wif { get; set; }

        public bool AddAddress()
        {
            if (!BitcoinUtilities.Wif.TryDecode(networkParameters.NetworkKind, Wif, out var privateKey, out bool _))
            {
                viewContext.ShowError("Invalid WIF encoding.");
                return false;
            }

            // todo: add only one address for the given compression flag?
            wallet.AddPrivateKey(privateKey);
            return true;
        }
    }
}