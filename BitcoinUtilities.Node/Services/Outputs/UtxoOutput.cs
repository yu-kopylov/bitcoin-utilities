using System;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Services.Outputs
{
    public class UtxoOutput
    {
        public UtxoOutput(TxOutPoint outputPoint, int height, ulong value, byte[] script, int spentHeight)
        {
            OutputPoint = outputPoint;
            Height = height;
            Value = value;
            Script = script;
            SpentHeight = spentHeight;
        }

        public UtxoOutput(byte[] txHash, int outputIndex, int height, TxOut txOut)
        {
            OutputPoint = new TxOutPoint(txHash, outputIndex);
            Height = height;
            Value = txOut.Value;
            Script = txOut.PubkeyScript;
            SpentHeight = -1;
        }

        public TxOutPoint OutputPoint { get; }
        public int Height { get; }
        public ulong Value { get; }
        public byte[] Script { get; }
        public int SpentHeight { get; }

        public UtxoOutput Spend(int spentHeight)
        {
            return new UtxoOutput(OutputPoint, Height, Value, Script, spentHeight);
        }
    }
}