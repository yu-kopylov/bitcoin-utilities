using System.Collections.Generic;
using BitcoinUtilities.P2P.Messages;

namespace BitcoinUtilities.Node.Events
{
    /// <summary>
    /// A notification from a service that it will require certain blocks,
    /// which it will later request with a <see cref="RequestBlockEvent"/>.
    /// </summary>
    public sealed class PrefetchBlocksEvent
    {
        // todo: describe owner
        // todo: pass DbHeader?
        public PrefetchBlocksEvent(object requestOwner, IEnumerable<byte[]> headerHashes)
        {
            RequestOwner = requestOwner;
            HeaderHashes = new List<byte[]>(headerHashes);
        }

        public object RequestOwner { get; }
        public IReadOnlyCollection<byte[]> HeaderHashes { get; }
    }

    /// <summary>
    /// <para> A request from a service to pass back a block with the given hash.</para>
    /// <para>
    /// The service that stores or downloads blocks must reply with <see cref="BlockAvailableEvent"/>
    /// if or when the requested block is available.
    /// </para>
    /// </summary>
    public sealed class RequestBlockEvent
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
    /// The content of the block that was previously requested with <see cref="RequestBlockEvent"/>.
    /// </summary>
    public sealed class BlockAvailableEvent
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