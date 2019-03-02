using System.Globalization;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.GUI.ViewModels.Wallet
{
    public class WalletOutputViewModel
    {
        public WalletOutputViewModel(NetworkParameters networkParameters, byte[] publicKeyHash, byte[] txHash, int outputIndex, ulong value, byte[] pubkeyScript)
        {
            Address = networkParameters.AddressConverter.ToDefaultAddress(publicKeyHash);
            PublicKeyHash = publicKeyHash;
            OutPoint = new TxOutPoint(txHash, outputIndex);
            Value = value;
            // todo: move formatting to view and use node / app parameters to set format
            FormattedValue = value.ToString("0,000", CultureInfo.InvariantCulture);
            PubkeyScript = pubkeyScript;
        }

        public string Address { get; set; }
        public byte[] PublicKeyHash { get; set; }
        public TxOutPoint OutPoint { get; set; }
        public ulong Value { get; set; }
        public string FormattedValue { get; }
        public byte[] PubkeyScript { get; }
    }
}