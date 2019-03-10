using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Scripts
{
    public class BitcoinCashSigHashCalculatorFactory : ISigHashCalculatorFactory
    {
        public ISigHashCalculator CreateCalculator(uint timestamp, Tx transaction)
        {
            if (timestamp >= 1510600000)
            {
                return new BitcoinCashSigHashCalculator(transaction);
            }

            return new BitcoinCoreSigHashCalculator(transaction);
        }
    }
}