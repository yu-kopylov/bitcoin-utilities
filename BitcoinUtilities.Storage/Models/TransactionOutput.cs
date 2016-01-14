namespace BitcoinUtilities.Storage.Models
{
    public class TransactionOutput
    {
        public long Id { get; set; }

        public Transaction Transaction { get; set; }

        public int NumberInTransaction { get; set; }

        public ulong Value { get; set; }

        public BinaryData PubkeyScript { get; set; }
        
        public Address Address { get; set; }
    }
}