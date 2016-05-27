namespace BitcoinUtilities.Storage.SQLite.Models
{
    public class BinaryData
    {
        public long Id { get; set; }

        public byte[] Data { get; set; }
    }
}