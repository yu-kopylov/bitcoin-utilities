namespace BitcoinUtilities.Storage.Models
{
    public class TransactionInput
    {
        public long Id { get; set; }

        public Transaction Transaction { get; set; }

        public byte[] SignatureScript { get; set; }

        public uint Sequence { get; set; }
        
        public TransactionOutput OutPoint { get; set; }
    }
}