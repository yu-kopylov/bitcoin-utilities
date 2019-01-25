using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Services.Outputs
{
    public class UtxoOutput : ISpendableOutput
    {
        public UtxoOutput(TxOutPoint outputPoint, int height, ulong value, byte[] pubkeyScript, int spentHeight)
        {
            OutputPoint = outputPoint;
            Height = height;
            Value = value;
            PubkeyScript = pubkeyScript;
            SpentHeight = spentHeight;
        }

        public UtxoOutput(byte[] txHash, int outputIndex, int height, TxOut txOut)
        {
            OutputPoint = new TxOutPoint(txHash, outputIndex);
            Height = height;
            Value = txOut.Value;
            PubkeyScript = txOut.PubkeyScript;
            SpentHeight = -1;
        }

        public TxOutPoint OutputPoint { get; }
        public int Height { get; }
        public ulong Value { get; }
        public byte[] PubkeyScript { get; }
        public int SpentHeight { get; }

        public UtxoOutput Spend(int spentHeight)
        {
            return new UtxoOutput(OutputPoint, Height, Value, PubkeyScript, spentHeight);
        }
    }
}