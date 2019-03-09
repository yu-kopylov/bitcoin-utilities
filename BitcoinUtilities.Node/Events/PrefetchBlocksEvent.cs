using System.Collections.Generic;
using BitcoinUtilities.Node.Modules.Headers;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Events
{
    /// <summary>
    /// A notification from a service that it will require certain blocks,
    /// which it will later request with a <see cref="RequestBlockEvent"/>.
    /// </summary>
    public sealed class PrefetchBlocksEvent : IEvent
    {
        // todo: describe owner
        // todo: pass DbHeader?
        public PrefetchBlocksEvent(object requestOwner, IEnumerable<DbHeader> headerHashes)
        {
            RequestOwner = requestOwner;
            Headers = new List<DbHeader>(headerHashes);
        }

        public object RequestOwner { get; }
        public IReadOnlyList<DbHeader> Headers { get; }
    }

    /// <summary>
    /// <para> A request from a service to pass back a block with the given hash.</para>
    /// <para>
    /// The service that stores or downloads blocks must reply with <see cref="BlockAvailableEvent"/>
    /// if or when the requested block is available.
    /// </para>
    /// </summary>
    public sealed class RequestBlockEvent : IEvent
    {
        // todo: describe owner
        // todo: pass DbHeader?
        public RequestBlockEvent(object requestOwner, byte[] hash)
        {
            RequestOwner = requestOwner;
            Hash = hash;
        }

        public object RequestOwner { get; }
        public byte[] Hash { get; }
    }

    /// <summary>
    /// A notification from a service that it downloaded a block that was not previously available.
    /// </summary>
    public sealed class BlockDownloadedEvent : IEvent
    {
        public BlockDownloadedEvent(byte[] headerHash, BlockMessage block)
        {
            HeaderHash = headerHash;
            Block = block;
        }

        public byte[] HeaderHash { get; }
        public BlockMessage Block { get; }
    }

    /// <summary>
    /// The content of the block that was previously requested with <see cref="RequestBlockEvent"/>.
    /// </summary>
    public sealed class BlockAvailableEvent : IEvent
    {
        public BlockAvailableEvent(byte[] headerHash, BlockMessage block)
        {
            HeaderHash = headerHash;
            Block = block;
        }

        public byte[] HeaderHash { get; }
        public BlockMessage Block { get; }
    }
}