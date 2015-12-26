namespace BitcoinUtilities.Storage.Models
{
    public class TransactionInput
    {
        public long Id { get; set; }

        public Transaction Transaction { get; set; }

        public int NumberInTransaction { get; set; }

        public byte[] SignatureScript { get; set; }

        public uint Sequence { get; set; }

        public byte[] OutputHash { get; set; }

        public uint OutputIndex { get; set; }

        public TransactionOutput Output { get; set; }
    }
}