using System;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.P2P;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Services
{
    public abstract class NodeEventHandlingService : EventHandlingService
    {
        private readonly BitcoinEndpoint endpoint;

        protected NodeEventHandlingService(BitcoinEndpoint endpoint)
        {
            this.endpoint = endpoint;
        }

        protected void OnMessage<T>(Action<T> handler) where T : IBitcoinMessage
        {
            On<MessageEvent>(evt => evt.Message is T && evt.Endpoint == endpoint, evt => handler((T) evt.Message));
        }
    }
}