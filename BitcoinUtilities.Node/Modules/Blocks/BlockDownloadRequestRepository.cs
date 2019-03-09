using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Node.Modules.Headers;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Modules.Blocks
{
    public class BlockDownloadRequestRepository
    {
        private readonly IEventDispatcher eventDispatcher;

        private readonly object monitor = new object();

        private readonly Dictionary<object, List<byte[]>> requestsByOwner = new Dictionary<object, List<byte[]>>(ReferenceEqualityComparer<object>.Instance);
        private readonly Dictionary<byte[], BlockDownloadState> statesByHash = new Dictionary<byte[], BlockDownloadState>(ByteArrayComparer.Instance);

        private readonly SortedSet<BlockDownloadState> nonReceivedBlocks = new SortedSet<BlockDownloadState>(
            Comparer<BlockDownloadState>.Create((s1, s2) => DbHeader.HeightHashComparer.Compare(s1.Header, s2.Header))
        );

        public BlockDownloadRequestRepository(IEventDispatcher eventDispatcher)
        {
            this.eventDispatcher = eventDispatcher;
        }

        public void AddRequest(object owner, IReadOnlyList<DbHeader> headers)
        {
            lock (monitor)
            {
                List<BlockDownloadState> statesWithCancelledRequests = new List<BlockDownloadState>();

                if (requestsByOwner.TryGetValue(owner, out var oldRequests))
                {
                    foreach (var hash in oldRequests)
                    {
                        BlockDownloadState state = statesByHash[hash];
                        state.RemoveRequest(owner);
                        statesWithCancelledRequests.Add(state);
                    }
                }

                requestsByOwner[owner] = new List<byte[]>(headers.Select(h => h.Hash));

                foreach (DbHeader header in headers)
                {
                    if (!statesByHash.TryGetValue(header.Hash, out var state))
                    {
                        state = new BlockDownloadState(header);
                        statesByHash.Add(header.Hash, state);
                        nonReceivedBlocks.Add(state);
                    }
                    else if (state.Header.Height != header.Height)
                    {
                        throw new InvalidOperationException(
                            $"Block with hash '{HexUtils.GetString(header.Hash)}' was requested using different heights: {state.Header.Height}, {header.Height}."
                        );
                    }

                    state.AddRequest(owner);
                }

                foreach (var state in statesWithCancelledRequests.Where(s => !s.HasRequests))
                {
                    statesByHash.Remove(state.Header.Hash);
                    nonReceivedBlocks.Remove(state);
                }

                eventDispatcher.Raise(new BlockDownloadRequestedEvent());
            }
        }

        public delegate IEnumerable<byte[]> SelectBlocks(IReadOnlyCollection<IBlockDownloadState> blocks);

        public void PickBlocksForDownload(BlockDownloadService downloader, SelectBlocks selector)
        {
            lock (monitor)
            {
                var pickedHashes = selector(nonReceivedBlocks);
                foreach (var hash in pickedHashes)
                {
                    statesByHash[hash].AddDownloader(downloader);
                }
            }
        }

        public void MarkReceived(byte[] hash)
        {
            lock (monitor)
            {
                if (statesByHash.TryGetValue(hash, out var state))
                {
                    nonReceivedBlocks.Remove(state);
                }
            }
        }

        private class BlockDownloadState : IBlockDownloadState
        {
            private readonly HashSet<object> requestOwners = new HashSet<object>(ReferenceEqualityComparer<object>.Instance);
            private readonly HashSet<BlockDownloadService> downloaders = new HashSet<BlockDownloadService>(ReferenceEqualityComparer<BlockDownloadService>.Instance);

            public BlockDownloadState(DbHeader header)
            {
                Header = header;
            }

            public DbHeader Header { get; }

            public bool HasRequests => requestOwners.Count != 0;

            public DateTime LastDownloadAttemptUtc { get; private set; }

            public void AddRequest(object owner)
            {
                requestOwners.Add(owner);
            }

            public void RemoveRequest(object owner)
            {
                requestOwners.Remove(owner);
            }

            public void AddDownloader(BlockDownloadService downloader)
            {
                //todo: check that LastDownloadAttemptUtc is not in future
                LastDownloadAttemptUtc = SystemTime.UtcNow;
                downloaders.Add(downloader);
            }
        }
    }

    public interface IBlockDownloadState
    {
        DbHeader Header { get; }
        DateTime LastDownloadAttemptUtc { get; }
    }
}