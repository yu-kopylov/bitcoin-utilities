using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Storage
{
    public class UnspentOutput
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

        public static UnspentOutput Create(int height, Tx transaction, int outputNumber)
        {
            TxOut output = transaction.Outputs[outputNumber];

            return new UnspentOutput(
                height,
                CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(transaction.Write)),
                outputNumber,
                output.Value,
                output.PubkeyScript);
        }
    }
}