using BitcoinUtilities.Node.Modules.Headers;
using BitcoinUtilities.Node.Rules;

namespace BitcoinUtilities.Node.Modules.Outputs.Events
{
    public class SignatureValidationRequest
    {
        public SignatureValidationRequest(DbHeader header, ProcessedTransaction[] transactions)
        {
            Header = header;
            Transactions = transactions;
        }

        public DbHeader Header { get; }
        public ProcessedTransaction[] Transactions { get; }
    }
}