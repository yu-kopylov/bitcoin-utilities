using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node.Services.Headers;

namespace BitcoinUtilities.Node.Services.Blocks
{
    public class BlockRequestCollection
    {
        private readonly object monitor = new object();

        // todo: use reference equality comparer
        private readonly Dictionary<object, List<byte[]>> requests = new Dictionary<object, List<byte[]>>();

        private readonly Dictionary<byte[], RequestedBlock> requestedBlocks = new Dictionary<byte[], RequestedBlock>(ByteArrayComparer.Instance);

        private List<DbHeader> pendingRequests;

        private readonly HashSet<byte[]> requiredBlocks = new HashSet<byte[]>(ByteArrayComparer.Instance);

        public void AddRequest(object owner, IReadOnlyCollection<DbHeader> headers)
        {
            lock (monitor)
            {
                // adding request before removing to avoid recreation of RequestedBlock instances with single request owner
                foreach (DbHeader header in headers)
                {
                    if (!requestedBlocks.TryGetValue(header.Hash, out var requestedBlock))
                    {
                        requestedBlock = new RequestedBlock(header);
                        requestedBlocks.Add(header.Hash, requestedBlock);
                    }

                    requestedBlock.RequestCount++;
                }

                if (requests.TryGetValue(owner, out var oldRequests))
                {
                    foreach (byte[] hash in oldRequests)
                    {
                        RequestedBlock requestedBlock = requestedBlocks[hash];
                        requestedBlock.RequestCount--;
                        if (requestedBlock.RequestCount == 0)
                        {
                            requestedBlocks.Remove(hash);
                        }
                    }
                }

                requests[owner] = headers.Select(h => h.Hash).ToList();

                pendingRequests = null;
            }
        }

        // todo: handle multiple owners
        public void SetRequired(byte[] hash)
        {
            lock (monitor)
            {
                requiredBlocks.Clear();
                requiredBlocks.Add(hash);
            }
        }

        public bool MarkReceived(byte[] hash)
        {
            lock (monitor)
            {
                if (requestedBlocks.TryGetValue(hash, out var requestedBlock))
                {
                    requestedBlock.Received = true;
                    pendingRequests = null;
                }

                return requiredBlocks.Remove(hash);
            }
        }

        public IReadOnlyCollection<DbHeader> GetPendingRequests()
        {
            lock (monitor)
            {
                if (pendingRequests == null)
                {
                    pendingRequests = requestedBlocks.Values.Where(b => !b.Received).Select(b => b.Header).OrderBy(h => h.Height).ToList();
                }

                return pendingRequests;
            }
        }

        private class RequestedBlock
        {
            public RequestedBlock(DbHeader header)
            {
                Header = header;
            }

            public DbHeader Header { get; }

            public int RequestCount { get; set; }

            public bool Received { get; set; }
        }
    }
}