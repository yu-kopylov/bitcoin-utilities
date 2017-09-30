namespace BitcoinUtilities.Scripts
{
    public interface ISigHashCalculator
    {
        int InputIndex { get; set; }
        ulong Value { get; set; }

        byte[] Calculate(SigHashType sigHashType, byte[] subScript);
    }
}