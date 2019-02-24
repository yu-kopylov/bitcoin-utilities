using System.Globalization;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.GUI.ViewModels.Wallet
{
    public class WalletOutputViewModel
    {
        public WalletOutputViewModel(string address, byte[] txHash, int outputIndex, ulong value, byte[] pubkeyScript)
        {
            Address = address;
            OutPoint = new TxOutPoint(txHash, outputIndex);
            Value = value;
            // todo: move formatting to view and use node / app parameters to set format
            FormattedValue = value.ToString("0,000", CultureInfo.InvariantCulture);
            PubkeyScript = pubkeyScript;
        }

        public string Address { get; set; }
        public TxOutPoint OutPoint { get; set; }
        public ulong Value { get; set; }
        public string FormattedValue { get; }
        public byte[] PubkeyScript { get; }
    }
}