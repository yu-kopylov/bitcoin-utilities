using BitcoinUtilities.P2P;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node
{
    /// <summary>
    /// A factory that produces services.
    /// </summary>
    public interface INodeEventServiceFactory
    {
        /// <summary>
        /// This method is called when new connection is established.
        /// </summary>
        /// <param name="node">The node that will use the service.</param>
        /// <param name="endpoint">The endpoint to the connected node.</param>
        /// <returns>An instace of the <see cref="IEventHandlingService"/>.</returns>
        IEventHandlingService CreateForEndpoint(BitcoinNode node, BitcoinEndpoint endpoint);
    }
}