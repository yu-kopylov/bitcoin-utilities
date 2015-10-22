namespace BitcoinUtilities.Storage.Models
{
    public class TransactionInput
    {
        public long Id { get; set; }

        public Transaction Transaction { get; set; }

        public int NumberInTransaction { get; set; }

        public byte[] SignatureScript { get; set; }

        public uint Sequence { get; set; }

        public byte[] SourceHash { get; set; }

        public uint SourceIndex { get; set; }

        public TransactionOutput Source { get; set; }
    }
}