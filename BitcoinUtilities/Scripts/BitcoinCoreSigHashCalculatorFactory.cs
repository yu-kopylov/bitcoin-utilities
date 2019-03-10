using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Scripts
{
    public class BitcoinCoreSigHashCalculatorFactory : ISigHashCalculatorFactory
    {
        public ISigHashCalculator CreateCalculator(uint timestamp, Tx transaction)
        {
            return new BitcoinCoreSigHashCalculator(transaction);
        }
    }
}