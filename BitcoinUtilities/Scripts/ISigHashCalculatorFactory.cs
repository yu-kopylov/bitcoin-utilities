using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Scripts
{
    public interface ISigHashCalculatorFactory
    {
        ISigHashCalculator CreateCalculator(uint timestamp, Tx transaction);
    }
}