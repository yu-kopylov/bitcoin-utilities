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
        /// Creates resources that will be used by services.
        /// </summary>
        /// <param name="node">The node that will host services that will use created resources.</param>
        void CreateResources(BitcoinNode node);

        /// <summary>
        /// This method is called diring the start of a <see cref="BitcoinNode"/>.
        /// </summary>
        /// <param name="node">The node that will use created services.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to stop long-running operations.</param>
        /// <returns>Instances of <see cref="IEventHandlingService"/>.</returns>
        IReadOnlyCollection<IEventHandlingService> CreateNodeServices(BitcoinNode node, CancellationToken cancellationToken);

        /// <summary>
        /// This method is called when a new connection is established.
        /// </summary>
        /// <param name="node">The node that will use created services.</param>
        /// <param name="endpoint">The endpoint to the connected node.</param>
        /// <returns>Instances of <see cref="IEventHandlingService"/>.</returns>
        IReadOnlyCollection<IEventHandlingService> CreateEndpointServices(BitcoinNode node, BitcoinEndpoint endpoint);
    }
}