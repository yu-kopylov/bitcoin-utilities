using System;
using System.Collections.Generic;
using System.IO;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Services.Headers
{
    /// <summary>
    /// An connected tree of headers.
    /// The tree is stored in memory, and update operations are mirrored in the given storage.
    /// </summary>
    public class Blockchain2
    {
        private readonly object monitor = new object();

        private readonly HeaderStorage storage;
        private readonly DbHeader root;

        private readonly Dictionary<byte[], DbHeader> headersByHash = new Dictionary<byte[], DbHeader>(ByteArrayComparer.Instance);
        private readonly Dictionary<byte[], List<byte[]>> headersByParent = new Dictionary<byte[], List<byte[]>>(ByteArrayComparer.Instance);
        private readonly HashSet<byte[]> heads = new HashSet<byte[]>();

        /// <summary>
        /// Initializes a new blockchain reading data from the given storage.
        /// If storage is empty, starts a new tree with the given genesis block header.
        /// </summary>
        /// <param name="storage">The storage to read headers from.</param>
        /// <param name="genesisBlockHeader">The header of the genesis block.</param>
        public Blockchain2(HeaderStorage storage, BlockHeader genesisBlockHeader)
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
        public IReadOnlyCollection<DbHeader> GetHeaders(IReadOnlyCollection<byte[]> hashes)
        {
            List<DbHeader> headers = new List<DbHeader>(hashes.Count);

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
        /// Adds the given headers to this blockchain.
        /// For each given header, its parent should either be already in the chain, or should precede that header in the given list.
        /// Headers that are already present in the chain are ignored.
        /// </summary>
        /// <param name="headers">A collection of headers to add.</param>
        /// <returns>A list of headers that was included into the blockchain.</returns>
        public IReadOnlyCollection<DbHeader> Add(IReadOnlyCollection<DbHeader> headers)
        {
            Dictionary<byte[], DbHeader> includedHeadersByHash = new Dictionary<byte[], DbHeader>(ByteArrayComparer.Instance);
            lock (monitor)
            {
                foreach (var header in headers)
                {
                    if (headersByHash.ContainsKey(header.Hash))
                    {
                        // header already exists
                        continue;
                    }

                    if (!headersByHash.TryGetValue(header.ParentHash, out var parentHeader) && !includedHeadersByHash.TryGetValue(header.ParentHash, out parentHeader))
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

                    includedHeadersByHash.Add(includedHeader.Hash, includedHeader);
                }

                var includedHeaders = new List<DbHeader>(includedHeadersByHash.Values);
                storage.AddHeaders(includedHeaders);

                foreach (var header in includedHeaders)
                {
                    AddUnsafe(header);
                }

                return includedHeaders;
            }
        }

        /// <summary>
        /// Marks sub-tree starting with the given header as invalid.
        /// Does nothing if the header with the given hash is not present in this blockchain.
        /// </summary>
        /// <param name="headerHash">The hash of the root header of the invalid sub-tree.</param>
        public void MarkInvalid(byte[] headerHash)
        {
            lock (monitor)
            {
                if (!headersByHash.ContainsKey(headerHash))
                {
                    // nothing to update
                    return;
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

            heads.Remove(header.ParentHash);
            heads.Add(header.Hash);
        }
    }
}