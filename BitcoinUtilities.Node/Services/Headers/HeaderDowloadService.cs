using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.Threading;
using NLog;

namespace BitcoinUtilities.Node.Services.Headers
{
    public class HeaderDowloadServiceFactory : INodeEventServiceFactory
    {
        public IEventHandlingService CreateForEndpoint(BitcoinNode node, BitcoinEndpoint endpoint)
        {
            return new HeaderDowloadService(node, endpoint);
        }
    }

    public class HeaderDowloadService : NodeEventHandlingService
    {
        private static readonly ILogger logger = LogManager.GetLogger(nameof(HeaderDowloadService));

        private readonly BitcoinNode node;
        private readonly BitcoinEndpoint endpoint;

        public HeaderDowloadService(BitcoinNode node, BitcoinEndpoint endpoint) : base(endpoint)
        {
            this.node = node;
            this.endpoint = endpoint;
            OnMessage<HeadersMessage>(OnHeadersReceived);
        }

        public override void OnStart()
        {
            base.OnStart();
            byte[][] blockLocator = GetLocator(node.Blockchain2.GetBestHead());
            endpoint.WriteMessage(new GetHeadersMessage(endpoint.ProtocolVersion, blockLocator, new byte[32]));
        }

        public void OnHeadersReceived(HeadersMessage message)
        {
            List<DbHeader> remainingHeaders = new List<DbHeader>(message.Headers.Length);

            foreach (var header in message.Headers)
            {
                // todo: we would not need write method if payload were passed with event
                byte[] hash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(header.Write));

                if (!BlockHeaderValidator.IsValid(header, hash))
                {
                    //todo: disconnect node
                    throw new BitcoinProtocolViolationException($"Invalid block header received. Hash: '{HexUtils.GetString(hash)}'.");
                }

                remainingHeaders.Add(new DbHeader(header, hash, -1, 0, true));
            }

            DbHeader bestHeader = null;

            var knownParents = node.Blockchain2.GetHeaders(remainingHeaders.Select(h => h.ParentHash));
            Dictionary<byte[], DbHeader> knownParentsByHash = new Dictionary<byte[], DbHeader>(ByteArrayComparer.Instance);
            foreach (DbHeader parent in knownParents)
            {
                knownParentsByHash[parent.Hash] = parent;
            }

            // Here we assume that headers in the message are already sorted by height, which should be true for most implementations.
            // We also expect no more than 2 branches in a single message.
            for (int chainNum = 0; chainNum < 2 && remainingHeaders.Count != 0; chainNum++)
            {
                HeaderSubchain subchain = null;
                List<DbHeader> newHeaders = new List<DbHeader>();
                List<DbHeader> orphanHeaders = new List<DbHeader>();
                foreach (var header in remainingHeaders)
                {
                    // todo: test performance of validation vs checking against knownParentsByHash

                    if (subchain == null && knownParentsByHash.ContainsKey(header.ParentHash))
                    {
                        subchain = node.Blockchain2.GetSubChain(header.ParentHash, BlockHeaderValidator.RequiredSubchainLength);
                    }

                    if (subchain == null || !ByteArrayComparer.Instance.Equals(subchain.Head.Hash, header.ParentHash))
                    {
                        orphanHeaders.Add(header);
                        continue;
                    }

                    DbHeader parentHeader = subchain.Head;
                    if (!parentHeader.IsValid)
                    {
                        //todo: disconnect node
                        throw new BitcoinProtocolViolationException(
                            $"Block header '{HexUtils.GetString(header.Hash)}' is a child of an invalid header '{HexUtils.GetString(parentHeader.Hash)}'."
                        );
                    }

                    DbHeader newHeader = header.AppendAfter(parentHeader, DifficultyUtils.DifficultyTargetToWork(DifficultyUtils.NBitsToTarget(header.NBits)));

                    if (!BlockHeaderValidator.IsValid(node.NetworkParameters.Fork, newHeader, subchain))
                    {
                        //todo: disconnect node
                        throw new BitcoinProtocolViolationException($"Invalid block header received. Hash: '{HexUtils.GetString(newHeader.Hash)}'.");
                    }

                    subchain.Append(newHeader);
                    newHeaders.Add(newHeader);
                }

                var savedHeaders = node.Blockchain2.Add(newHeaders);
                foreach (DbHeader header in savedHeaders)
                {
                    // todo: use is better method
                    if (bestHeader == null || header.TotalWork > bestHeader.TotalWork)
                    {
                        bestHeader = header;
                    }
                }

                remainingHeaders = orphanHeaders;
            }

            if (remainingHeaders.Count > 0)
            {
                // todo: test
            }

            logger.Debug(() =>
                $"Received headers from endpoint '{endpoint.PeerInfo.IpEndpoint}'. " +
                $"Best header: (Height: {bestHeader?.Height}, Hash: {(bestHeader == null ? "null" : HexUtils.GetString(bestHeader.Hash))})."
            );

            if (bestHeader != null)
            {
                var blockLocator = GetLocator(bestHeader);
                logger.Debug(() =>
                    $"Requesting headers from endpoint '{endpoint.PeerInfo.IpEndpoint}'." +
                    $"Best hash: {HexUtils.GetString(blockLocator[0])})."
                );
                endpoint.WriteMessage(new GetHeadersMessage(endpoint.ProtocolVersion, blockLocator, new byte[32]));
            }
        }

        private byte[][] GetLocator(DbHeader knownHeader)
        {
            List<DbHeader> locatorHeaders = new List<DbHeader>();

            locatorHeaders.AddRange(node.Blockchain2.GetSubChain(knownHeader.ParentHash, 999) ?? Enumerable.Empty<DbHeader>());
            locatorHeaders.Add(knownHeader);

            DbHeader bestHead = node.Blockchain2.GetBestHead(knownHeader.Hash);
            if (bestHead != null && bestHead.Height != knownHeader.Height)
            {
                // todo: test
                int bestRelatedChainLength = Math.Min(bestHead.Height - knownHeader.Height, 1000);
                var bestRelatedChain = node.Blockchain2.GetSubChain(bestHead.Hash, bestRelatedChainLength);
                if (bestRelatedChain != null)
                {
                    locatorHeaders.AddRange(bestRelatedChain);
                }
            }

            locatorHeaders.Reverse();

            return locatorHeaders.Select(h => h.Hash).ToArray();
        }
    }
}