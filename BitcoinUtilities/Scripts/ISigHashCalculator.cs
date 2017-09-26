namespace BitcoinUtilities.Scripts
{
    public interface ISigHashCalculator
    {
        byte[] Calculate(SigHashType sigHashType, byte[] subScript);
    }
}