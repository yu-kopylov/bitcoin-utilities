using System.Collections.Generic;

namespace BitcoinUtilities.Storage.SQLite.Models
{
    public class Transaction
    {
        public long Id { get; set; }

        public Block Block { get; set; }

        public int NumberInBlock { get; set; }

        public TransactionHash Hash { get; set; }

        public List<TransactionInput> Inputs { get; set; }
        
        public List<TransactionOutput> Outputs { get; set; }
    }
}