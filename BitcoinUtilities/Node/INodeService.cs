using BitcoinUtilities.P2P;

namespace BitcoinUtilities.Node
{
    /// <summary>
    /// An interface for threads that provide services for <see cref="BitcoinNode"/>.
    /// <para/>
    /// <see cref="BitcoinNode"/> creates service using <see cref="INodeServiceFactory"/> during startup and shutdowns services when stopping.
    /// <para/>
    /// If a service implements <see cref="System.IDisposable"/>
    /// then <see cref="BitcoinNode"/> will dispose service after the service is stopped.
    /// </summary>
    public interface INodeService
    {
        /// <summary>
        /// The main method that would be executed in the thread.
        /// </summary>
        void Run();

        /// <summary>
        /// This method is called when a message is received by the node.
        /// </summary>
        /// <param name="endpoint">The endpoint that sent message.</param>
        /// <param name="message">The incoming message.</param>
        void ProcessMessage(BitcoinEndpoint endpoint, IBitcoinMessage message);
    }
}