namespace BitcoinUtilities.Storage.Models
{
    public class TransactionOutput
    {
        public long Id { get; set; }

        public Transaction Transaction { get; set; }

        public ulong Value { get; set; }

        public PubkeyScriptType ScriptType { get; set; }

        public byte[] PubkeyScript { get; set; }
        
        public byte[] PublicKey { get; set; }
    }
}