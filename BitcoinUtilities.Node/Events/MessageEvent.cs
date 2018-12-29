using BitcoinUtilities.P2P;

namespace BitcoinUtilities.Node.Events
{
    /// <summary>
    /// Represents event of receiving a message from a connected node.
    /// </summary>
    public class MessageEvent
    {
        public MessageEvent(BitcoinEndpoint endpoint, IBitcoinMessage message)
        {
            Endpoint = endpoint;
            Message = message;
        }

        public BitcoinEndpoint Endpoint { get; }
        public IBitcoinMessage Message { get; }
    }
}