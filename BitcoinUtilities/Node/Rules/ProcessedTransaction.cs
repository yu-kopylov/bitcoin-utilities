using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Rules
{
    public class ProcessedTransaction
    {
        public ProcessedTransaction(Tx transaction, byte[] txHash, TransactionInput[] inputs)
        {
            Transaction = transaction;
            TxHash = txHash;
            Inputs = inputs;
        }

        public Tx Transaction { get; }
        public byte[] TxHash { get; }
        public TransactionInput[] Inputs { get; }
    }
}