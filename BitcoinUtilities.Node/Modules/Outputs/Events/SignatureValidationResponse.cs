using BitcoinUtilities.Node.Modules.Headers;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Modules.Outputs.Events
{
    public class SignatureValidationResponse : IEvent
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