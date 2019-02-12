using BitcoinUtilities.Node.Modules.Headers;

namespace BitcoinUtilities.Node.Modules.Outputs.Events
{
    public class SignatureValidationResponse
    {
        public SignatureValidationResponse(DbHeader header, bool valid)
        {
            Header = header;
            Valid = valid;
        }

        public DbHeader Header { get; }
        public bool Valid { get; }
    }
}