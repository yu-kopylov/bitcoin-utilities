using System.Collections.Generic;

namespace BitcoinUtilities.Storage.Models
{
    public class Transaction
    {
        public long Id { get; set; }

        public byte[] Hash { get; set; }

        public Block Block { get; set; }

        public int NumberInBlock { get; set; }

        public List<TransactionInput> Inputs { get; set; }
        
        public List<TransactionOutput> Outputs { get; set; }
    }
}