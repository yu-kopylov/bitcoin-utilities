using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Modules.Wallet
{
    public class WalletOutput : ISpendableOutput
    {
        public WalletOutput(string address, byte[] txHash, int outputIndex, ulong value, byte[] pubkeyScript)
        {
            Address = address;
            OutPoint = new TxOutPoint(txHash, outputIndex);
            Value = value;
            PubkeyScript = pubkeyScript;
        }

        public string Address { get; set; }
        public TxOutPoint OutPoint { get; set; }
        public ulong Value { get; set; }
        public byte[] PubkeyScript { get; set; }
    }
}