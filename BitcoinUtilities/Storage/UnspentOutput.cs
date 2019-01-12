using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Storage
{
    public class UnspentOutput : ISpendableOutput
    {
        public UnspentOutput(int sourceBlockHeight, byte[] transactionHash, int outputNumber, ulong sum, byte[] publicScript)
        {
            SourceBlockHeight = sourceBlockHeight;
            TransactionHash = transactionHash;
            OutputNumber = outputNumber;
            Sum = sum;
            PublicScript = publicScript;
        }

        public int SourceBlockHeight { get; }
        public byte[] TransactionHash { get; }
        public int OutputNumber { get; }
        public ulong Sum { get; }
        public byte[] PublicScript { get; }

        ulong ISpendableOutput.Value => Sum;

        public static UnspentOutput Create(byte[] txHash, int outputIndex, int height, TxOut txOut)
        {
            return new UnspentOutput(
                height,
                txHash,
                outputIndex,
                txOut.Value,
                txOut.PubkeyScript
            );
        }
    }
}