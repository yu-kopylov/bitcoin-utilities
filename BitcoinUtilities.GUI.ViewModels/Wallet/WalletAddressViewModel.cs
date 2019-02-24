namespace BitcoinUtilities.GUI.ViewModels.Wallet
{
    public class WalletAddressViewModel
    {
        public WalletAddressViewModel(string address)
        {
            Address = address;
        }

        public string Address { get; }
    }
}