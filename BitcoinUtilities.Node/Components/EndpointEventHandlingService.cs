using System;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.P2P;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Components
{
    public abstract class EndpointEventHandlingService : EventHandlingService
    {
        private readonly BitcoinEndpoint endpoint;

        protected EndpointEventHandlingService(BitcoinEndpoint endpoint)
        {
            this.endpoint = endpoint;
        }

        protected void OnMessage<T>(Action<T> handler) where T : IBitcoinMessage
        {
            On<MessageEvent>(evt => evt.Message is T && evt.Endpoint == endpoint, evt => handler((T) evt.Message));
        }
    }
}