using System.Threading;

namespace BitcoinUtilities.Node
{
    /// <summary>
    /// A factory that produces 
    /// </summary>
    public interface INodeServiceFactory
    {
        /// <summary>
        /// This method is called diring the start of a <see cref="BitcoinNode"/>.
        /// </summary>
        /// <param name="node">The node that will use the service.</param>
        /// <param name="cancellationToken">A cancellation token that is used by the <see cref="BitcoinNode"/> to stop the service thread.</param>
        /// <returns>An instace of the <see cref="INodeService"/>.</returns>
        INodeService Create(BitcoinNode node, CancellationToken cancellationToken);
    }
}