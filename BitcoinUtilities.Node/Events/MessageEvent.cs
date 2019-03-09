using BitcoinUtilities.P2P;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Events
{
    /// <summary>
    /// Represents event of receiving a message from a connected node.
    /// </summary>
    public sealed class MessageEvent : IEvent
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