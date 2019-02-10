namespace BitcoinUtilities.Node.Services.Outputs
{
    public class UtxoOperation
    {
        public UtxoOperation(bool spent, UtxoOutput output)
        {
            Spent = spent;
            Output = output;
        }

        public bool Spent { get; }
        public UtxoOutput Output { get; }

        public static UtxoOperation Create(UtxoOutput output)
        {
            return new UtxoOperation(false, output);
        }

        public static UtxoOperation Spend(UtxoOutput output)
        {
            return new UtxoOperation(true, output);
        }
    }
}