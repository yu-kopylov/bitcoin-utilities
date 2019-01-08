using System;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Services.Outputs
{
    public class UtxoOutput
    {
        public UtxoOutput(byte[] outputPoint, int height, ulong value, byte[] script, int spentHeight)
        {
            OutputPoint = outputPoint;
            Height = height;
            Value = value;
            Script = script;
            SpentHeight = spentHeight;
        }

        public UtxoOutput(byte[] txHash, int outputIndex, int height, TxOut txOut)
        {
            OutputPoint = CreateOutPoint(txHash, outputIndex);
            Height = height;
            Value = txOut.Value;
            Script = txOut.PubkeyScript;
            SpentHeight = -1;
        }

        public byte[] OutputPoint { get; }
        public int Height { get; }
        public ulong Value { get; }
        public byte[] Script { get; }
        public int SpentHeight { get; }

        public UtxoOutput Spend(int spentHeight)
        {
            return new UtxoOutput(OutputPoint, Height, Value, Script, spentHeight);
        }

        public static byte[] CreateOutPoint(TxOutPoint outPoint)
        {
            return CreateOutPoint(outPoint.Hash, outPoint.Index);
        }

        public static byte[] CreateOutPoint(byte[] txHash, int outputIndex)
        {
            int hashLength = txHash.Length;
            byte[] outPoint = new byte[hashLength + 4];
            Array.Copy(txHash, outPoint, hashLength);
            outPoint[hashLength + 0] = (byte) outputIndex;
            outPoint[hashLength + 1] = (byte) (outputIndex >> 8);
            outPoint[hashLength + 2] = (byte) (outputIndex >> 16);
            outPoint[hashLength + 3] = (byte) (outputIndex >> 24);
            return outPoint;
        }
    }
}