namespace BitcoinUtilities.Node.Rules
{
    public interface ISpendableOutput
    {
        ulong Value { get; }
        byte[] PubkeyScript { get; }
    }
}