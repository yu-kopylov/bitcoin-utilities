namespace BitcoinUtilities.Storage.SQLite.Models
{
    public class TransactionHash
    {
        public long Id { get; set; }

        public byte[] Hash { get; set; }
        
        public uint SemiHash { get; set; }
    }
}