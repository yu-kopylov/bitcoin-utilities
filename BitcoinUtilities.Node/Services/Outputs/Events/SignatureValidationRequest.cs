using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.Node.Services.Headers;

namespace BitcoinUtilities.Node.Services.Outputs.Events
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