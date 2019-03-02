using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Modules.Wallet
{
    public class WalletOutput : ISpendableOutput
    {
        public WalletOutput(WalletAddress walletAddress, byte[] publicKeyHash, byte[] txHash, int outputIndex, ulong value, byte[] pubkeyScript)
        {
            WalletAddress = walletAddress;
            PublicKeyHash = publicKeyHash;
            OutPoint = new TxOutPoint(txHash, outputIndex);
            Value = value;
            PubkeyScript = pubkeyScript;
        }

        public WalletAddress WalletAddress { get; }
        public byte[] PublicKeyHash { get; set; }
        public TxOutPoint OutPoint { get; set; }
        public ulong Value { get; set; }
        public byte[] PubkeyScript { get; set; }
    }
}