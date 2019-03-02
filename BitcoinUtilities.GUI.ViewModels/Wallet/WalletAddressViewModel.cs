using System.Globalization;
using BitcoinUtilities.Node.Modules.Wallet;

namespace BitcoinUtilities.GUI.ViewModels.Wallet
{
    public class WalletAddressViewModel
    {
        public WalletAddressViewModel(NetworkParameters networkParameters, WalletAddress address)
        {
            Address = networkParameters.AddressConverter.ToDefaultAddress(address.PrimaryPublicKeyHash);
            Balance = address.Balance.ToString("0,000", CultureInfo.InvariantCulture);
        }

        public string Address { get; }
        public string Balance { get; }
    }
}