namespace BitcoinUtilities.Storage.Models
{
    public class TransactionOutput
    {
        public long Id { get; set; }

        public Transaction Transaction { get; set; }

        public int NumberInTransaction { get; set; }

        public ulong Value { get; set; }

        public PubkeyScriptType PubkeyScriptType { get; set; }

        public byte[] PubkeyScript { get; set; }
        
        public byte[] PublicKey { get; set; }
    }
}