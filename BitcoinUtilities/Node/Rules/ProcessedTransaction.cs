using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Rules
{
    public class ProcessedTransaction
    {
        public ProcessedTransaction(Tx transaction, TransactionInput[] inputs)
        {
            Transaction = transaction;
            Inputs = inputs;
        }

        public Tx Transaction { get; }
        public TransactionInput[] Inputs { get; }
    }
}