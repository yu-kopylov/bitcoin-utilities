using BitcoinUtilities.Node.Services.Headers;

namespace BitcoinUtilities.Node.Services.Outputs.Events
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