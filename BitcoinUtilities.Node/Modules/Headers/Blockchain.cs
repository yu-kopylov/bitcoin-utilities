using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Modules.Headers
{
    /// <summary>
    /// An connected tree of headers.
    /// The tree is stored in memory, and update operations are mirrored in the given storage.
    /// </summary>
    public class Blockchain
    {
        private readonly object monitor = new object();

        private readonly HeaderStorage storage;
        private readonly DbHeader root;

        private readonly Dictionary<byte[], DbHeader> headersByHash = new Dictionary<byte[], DbHeader>(ByteArrayComparer.Instance);
        private readonly Dictionary<byte[], List<byte[]>> headersByParent = new Dictionary<byte[], List<byte[]>>(ByteArrayComparer.Instance);
        private readonly HashSet<byte[]> validHeads = new HashSet<byte[]>(ByteArrayComparer.Instance);
        private DbHeader bestHead;

        /// <summary>
        /// Initializes a new blockchain reading data from the given storage.
        /// If storage is empty, starts a new tree with the given genesis block header.
        /// </summary>
        /// <param name="storage">The storage to read headers from.</param>
        /// <param name="genesisBlockHeader">The header of the genesis block.</param>
        public Blockchain(HeaderStorage storage, BlockHeader genesisBlockHeader)
        {
            this.storage = storage;
            this.root = new DbHeader(genesisBlockHeader, CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(genesisBlockHeader.Write)), 0, 0, true);

            bool foundFirstBlock = false;
            foreach (var header in storage.ReadAll())
            {
                if (!foundFirstBlock)
                {
                    if (!ByteArrayComparer.Instance.Equals(header.Hash, root.Hash))
                    {
                        throw new IOException(
                            $"Header storage has incorrect first block. " +
                            $"Expected: '{HexUtils.GetString(root.Hash)}', but was '{HexUtils.GetString(header.Hash)}'."
                        );
                    }

                    AddUnsafe(header);
                    foundFirstBlock = true;
                }
                else
                {
                    if (!headersByHash.ContainsKey(header.ParentHash))
                    {
                        throw new IOException(
                            $"Header storage has block with missing parent. " +
                            $"Block: '{HexUtils.GetString(header.Hash)}'. " +
                            $"Parent: '{HexUtils.GetString(header.ParentHash)}'."
                        );
                    }

                    AddUnsafe(header);
                }
            }

            if (!foundFirstBlock)
            {
                storage.AddHeaders(new DbHeader[] {root});
                AddUnsafe(root);
            }
        }

        /// <summary>
        /// Gets blocks with the given hashes from this chain. Unknown hashes are skipped.
        /// </summary>
        /// <param name="hashes">The hashes of headers to return.</param>
        /// <returns>A list of headers with matching hashes.</returns>
        public IReadOnlyCollection<DbHeader> GetHeaders(IEnumerable<byte[]> hashes)
        {
            List<DbHeader> headers = new List<DbHeader>();

            lock (monitor)
            {
                foreach (byte[] hash in hashes)
                {
                    if (headersByHash.TryGetValue(hash, out var header))
                    {
                        headers.Add(header);
                    }
                }
            }

            return headers;
        }

        /// <summary>
        /// Returns all valid headers that do not have children.
        /// </summary>
        public IReadOnlyCollection<DbHeader> GetValidHeads()
        {
            lock (monitor)
            {
                return validHeads.Select(hash => headersByHash[hash]).ToList();
            }
        }

        /// <summary>
        /// Returns the best valid header.
        /// </summary>
        public DbHeader GetBestHead()
        {
            lock (monitor)
            {
                if (bestHead == null)
                {
                    bestHead = DbHeader.BestOf(validHeads.Select(hash => headersByHash[hash]));
                }

                return bestHead;
            }
        }

        /// <summary>
        /// <para>Returns the best valid header among descendats of the header with given hash.</para>
        /// <para>Returns null if a header with the given hash is not present in the blockchain or is invalid.</para>
        /// </summary>
        /// <param name="hash">The hash of the parent block.</param>
        public DbHeader GetBestHead(byte[] hash)
        {
            lock (monitor)
            {
                if (!headersByHash.TryGetValue(hash, out var rootHeader) || !rootHeader.IsValid)
                {
                    return null;
                }

                DbHeader bestHeader = null;
                Queue<byte[]> queue = new Queue<byte[]>();
                queue.Enqueue(hash);
                while (queue.Count != 0)
                {
                    hash = queue.Dequeue();

                    bool hasValidChild = false;
                    if (headersByParent.TryGetValue(hash, out var children))
                    {
                        foreach (byte[] childHash in children)
                        {
                            if (headersByHash[childHash].IsValid)
                            {
                                hasValidChild = true;
                                queue.Enqueue(childHash);
                            }
                        }
                    }

                    if (!hasValidChild)
                    {
                        DbHeader header = headersByHash[hash];
                        if (bestHeader == null || header.IsBetterThan(bestHeader))
                        {
                            bestHeader = header;
                        }
                    }
                }

                return bestHeader;
            }
        }

        /// <summary>
        /// <para>Returns a chain of headers ending at the header with the given hash.</para>
        /// <para>The returned chain can be shorter than the given length in case of the chain starting at the root of the blockchain tree.</para>
        /// </summary>
        /// <param name="hash">The hash of the last block.</param>
        /// <param name="length">The expected length.</param>
        /// <returns>The requested subchain if it exists; otherwise, null.</returns>
        public HeaderSubChain GetSubChain(byte[] hash, int length)
        {
            lock (monitor)
            {
                if (!headersByHash.TryGetValue(hash, out var header))
                {
                    return null;
                }

                List<DbHeader> headers = new List<DbHeader>();
                headers.Add(header);
                for (int i = 1; i < length; i++)
                {
                    if (!headersByHash.TryGetValue(header.ParentHash, out header))
                    {
                        break;
                    }

                    headers.Add(header);
                }

                headers.Reverse();
                return new HeaderSubChain(headers);
            }
        }

        /// <summary>
        /// <para>Adds the given headers to this blockchain.</para>
        /// <para>For each given header, its parent should either be already in the chain, or should precede that header in the given list.</para>
        /// <para>Headers that are already present in the chain are ignored.</para>
        /// <para>All given headers must be marked as valid.</para>
        /// </summary>
        /// <param name="headers">A collection of headers to add.</param>
        /// <returns>
        /// A list of headers that was included into the blockchain,
        /// including headers that were present in blockchain before the call to this method.
        /// </returns>
        public IReadOnlyCollection<DbHeader> Add(IReadOnlyCollection<DbHeader> headers)
        {
            Dictionary<byte[], DbHeader> newHeadersByHash = new Dictionary<byte[], DbHeader>(ByteArrayComparer.Instance);
            List<DbHeader> savedHeaders = new List<DbHeader>();
            lock (monitor)
            {
                foreach (var header in headers)
                {
                    if (headersByHash.TryGetValue(header.Hash, out var existingHeader))
                    {
                        savedHeaders.Add(existingHeader);
                        continue;
                    }

                    if (!headersByHash.TryGetValue(header.ParentHash, out var parentHeader) && !newHeadersByHash.TryGetValue(header.ParentHash, out parentHeader))
                    {
                        throw new InvalidOperationException(
                            $"The header '{HexUtils.GetString(header.Hash)}' cannot be added to the blockchain, " +
                            $"because the parent header '{HexUtils.GetString(header.ParentHash)}' is not present in the chain."
                        );
                    }

                    var includedHeader = header;
                    if (!parentHeader.IsValid)
                    {
                        includedHeader = includedHeader.MarkInvalid();
                    }

                    newHeadersByHash.Add(includedHeader.Hash, includedHeader);
                    savedHeaders.Add(includedHeader);
                }

                var newHeaders = new List<DbHeader>(newHeadersByHash.Values);
                storage.AddHeaders(newHeaders);

                foreach (var header in newHeaders)
                {
                    AddUnsafe(header);
                }

                return savedHeaders;
            }
        }

        /// <summary>
        /// <para>Marks sub-tree starting with the given header as invalid.</para>
        /// <para>Does nothing if the header with the given hash is not present in this blockchain.</para>
        /// <remarks>
        /// It is good to mark headers as invalid and keep them for some time instead of deleting them
        /// to avoid repeated download of valid headers that have invalid transactions.
        /// </remarks>
        /// </summary>
        /// <param name="headerHash">The hash of the root header of the invalid sub-tree.</param>
        public void MarkInvalid(byte[] headerHash)
        {
            lock (monitor)
            {
                if (!headersByHash.TryGetValue(headerHash, out var removalRoot) || !removalRoot.IsValid)
                {
                    // nothing to update
                    return;
                }

                if (removalRoot.Height == 0)
                {
                    throw new InvalidOperationException("The root of the blockchain cannot be marked as invalid.");
                }

                List<DbHeader> affectedHeaders = new List<DbHeader>();
                Queue<byte[]> queue = new Queue<byte[]>();
                queue.Enqueue(headerHash);
                while (queue.Count > 0)
                {
                    byte[] hash = queue.Dequeue();
                    var header = headersByHash[hash];
                    if (header.IsValid)
                    {
                        affectedHeaders.Add(header.MarkInvalid());
                        if (headersByParent.TryGetValue(hash, out var children))
                        {
                            foreach (byte[] child in children)
                            {
                                queue.Enqueue(child);
                            }
                        }
                    }
                }

                storage.MarkInvalid(affectedHeaders);

                foreach (DbHeader header in affectedHeaders)
                {
                    headersByHash[header.Hash] = header;
                    validHeads.Remove(header.Hash);
                }

                // New head candidate always exists, since root cannot be marked as invalid.
                // New head candidate is always valid, since removalRoot was valid.
                var newHeadCandidate = headersByHash[removalRoot.ParentHash];
                // New head candidate has at least removalRoot as a child.
                bool newHeadCandidateHasValidChildren = headersByParent[newHeadCandidate.Hash].Any(hash => headersByHash[hash].IsValid);
                if (!newHeadCandidateHasValidChildren)
                {
                    validHeads.Add(newHeadCandidate.Hash);
                }

                if (bestHead != null && !validHeads.Contains(bestHead.Hash))
                {
                    bestHead = null;
                }
            }
        }

        private void AddUnsafe(DbHeader header)
        {
            headersByHash.Add(header.Hash, header);
            if (!headersByParent.TryGetValue(header.ParentHash, out var siblings))
            {
                siblings = new List<byte[]>();
                headersByParent.Add(header.ParentHash, siblings);
            }

            siblings.Add(header.Hash);

            if (header.IsValid)
            {
                validHeads.Remove(header.ParentHash);
                validHeads.Add(header.Hash);

                // The only case when bestHead is removed from validHeads is when it is parent of this block.
                // Is this case it will be replaced by the condition below, because valid child header is always better than its parent.
                if (bestHead != null && header.IsBetterThan(bestHead))
                {
                    bestHead = header;
                }
            }
        }
    }
}