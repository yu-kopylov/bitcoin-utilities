namespace BitcoinUtilities.Storage
{
    public class SpentOutput
    {
        public SpentOutput(int sourceBlockHeight, int spentBlockHeight, byte[] transactionHash, int outputNumber, ulong sum, byte[] publicScript)
        {
            SourceBlockHeight = sourceBlockHeight;
            SpentBlockHeight = spentBlockHeight;
            TransactionHash = transactionHash;
            OutputNumber = outputNumber;
            Sum = sum;
            PublicScript = publicScript;
        }

        public int SourceBlockHeight { get; }
        public int SpentBlockHeight { get; }
        public byte[] TransactionHash { get; }
        public int OutputNumber { get; }
        public ulong Sum { get; }
        public byte[] PublicScript { get; }

        public static SpentOutput Create(StoredBlock block, UnspentOutput unspentOutput)
        {
            return new SpentOutput(
                unspentOutput.SourceBlockHeight,
                block.Height,
                unspentOutput.TransactionHash,
                unspentOutput.OutputNumber,
                unspentOutput.Sum,
                unspentOutput.PublicScript);
        }
    }
}