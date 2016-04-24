using System.Threading;

namespace BitcoinUtilities.Node
{
    /// <summary>
    /// An interface for threads that provide services for <see cref="BitcoinNode"/>.
    /// <see cref="BitcoinNode"/> manages start and shutdown of the services.
    /// <para/>
    /// If a service also implements <see cref="System.IDisposable"/>
    /// then <see cref="BitcoinNode"/> will dispose service on dispose of the <see cref="BitcoinNode"/>.
    /// </summary>
    public interface INodeService
    {
        /// <summary>
        /// This method is called once diring the start of bitcoin node.
        /// </summary>
        /// <param name="node">The node that starts the service.</param>
        /// <param name="cancellationToken">A cancellation token that is used by the BitcoinNode to stop the service thread.</param>
        void Init(BitcoinNode node, CancellationToken cancellationToken);

        /// <summary>
        /// The main method that would be executed in the thread.
        /// </summary>
        void Run();
    }
}