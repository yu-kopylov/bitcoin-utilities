namespace BitcoinUtilities.Node.Rules
{
    public class TransactionInput
    {
        public TransactionInput(ulong value, byte[] pubKeyScript)
        {
            Value = value;
            PubKeyScript = pubKeyScript;
        }

        public ulong Value { get; }
        public byte[] PubKeyScript { get; }
    }
}