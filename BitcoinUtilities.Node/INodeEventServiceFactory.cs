using System.Collections.Generic;
using System.Threading;
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
        /// This method is called diring the start of a <see cref="BitcoinNode"/>.
        /// </summary>
        /// <param name="node">The node that will use the service.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>Instances of <see cref="IEventHandlingService"/>.</returns>
        IReadOnlyCollection<IEventHandlingService> CreateForNode(BitcoinNode node, CancellationToken cancellationToken);

        /// <summary>
        /// This method is called when new connection is established.
        /// </summary>
        /// <param name="node">The node that will use the service.</param>
        /// <param name="endpoint">The endpoint to the connected node.</param>
        /// <returns>Instances of <see cref="IEventHandlingService"/>.</returns>
        IReadOnlyCollection<IEventHandlingService> CreateForEndpoint(BitcoinNode node, BitcoinEndpoint endpoint);
    }
}