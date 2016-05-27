namespace BitcoinUtilities.Storage.SQLite.Models
{
    public class TransactionInput
    {
        public long Id { get; set; }

        public Transaction Transaction { get; set; }

        public int NumberInTransaction { get; set; }

        public BinaryData SignatureScript { get; set; }

        public uint Sequence { get; set; }

        public TransactionHash OutputHash { get; set; }

        public uint OutputIndex { get; set; }
    }
}