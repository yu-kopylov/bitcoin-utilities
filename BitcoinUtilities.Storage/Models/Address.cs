namespace BitcoinUtilities.Storage.Models
{
    public class Address
    {
        public long Id { get; set; }

        public string Value { get; set; }

        public uint SemiHash { get; set; }
    }
}