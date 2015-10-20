using System.Collections.Generic;

namespace BitcoinUtilities.Storage.Models
{
    public class Block
    {
        public long Id { get; set; }

        public byte[] Hash { get; set; }

        public int Height { get; set; }

        public byte[] Header { get; set; }

        public List<Transaction> Transactions { get; set; }
    }
}