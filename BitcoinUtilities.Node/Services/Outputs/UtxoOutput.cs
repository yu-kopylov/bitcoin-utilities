using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Services.Outputs
{
    public class UtxoOutput : ISpendableOutput
    {
        public UtxoOutput(TxOutPoint outPoint, ulong value, byte[] pubkeyScript)
        {
            OutPoint = outPoint;
            Value = value;
            PubkeyScript = pubkeyScript;
        }

        public TxOutPoint OutPoint { get; }

        public ulong Value { get; }
        public byte[] PubkeyScript { get; }
    }
}