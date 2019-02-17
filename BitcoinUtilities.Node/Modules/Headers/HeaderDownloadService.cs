using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node.Components;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Threading;
using NLog;

namespace BitcoinUtilities.Node.Modules.Headers
{
    public class HeaderDownloadService : EndpointEventHandlingService
    {
        private static readonly ILogger logger = LogManager.GetLogger(nameof(HeaderDownloadService));

        private readonly IEventDispatcher eventDispatcher;
        private readonly Blockchain blockchain;
        private readonly NetworkParameters networkParameters;
        private readonly BitcoinEndpoint endpoint;

        public HeaderDownloadService(IEventDispatcher eventDispatcher, Blockchain blockchain, NetworkParameters networkParameters, BitcoinEndpoint endpoint) : base(endpoint)
        {
            this.eventDispatcher = eventDispatcher;
            this.blockchain = blockchain;
            this.networkParameters = networkParameters;
            this.endpoint = endpoint;
            OnMessage<HeadersMessage>(OnHeadersReceived);
        }

        public override void OnStart()
        {
            base.OnStart();
            byte[][] blockLocator = GetLocator(blockchain.GetBestHead());
            endpoint.WriteMessage(new GetHeadersMessage(endpoint.ProtocolVersion, blockLocator, new byte[32]));
        }

        public void OnHeadersReceived(HeadersMessage message)
        {
            var bestHeadBeforeUpdate = blockchain.GetBestHead();

            List<DbHeader> remainingHeaders = CreateAndValidateDbHeaders(message.Headers);
            Dictionary<byte[], DbHeader> knownParentsByHash = FetchKnownParents(remainingHeaders);

            DbHeader bestHeader = null;

            // Here we assume that headers in the message are already sorted by height, which should be true for most implementations.
            // We also expect no more than 2 branches in a single message.
            for (int chainNum = 0; chainNum < 2 && remainingHeaders.Count != 0; chainNum++)
            {
                var newChain = ExtractAndValidateSingleChain(remainingHeaders, knownParentsByHash, out var orphanHeaders);

                if (newChain.Count == 0)
                {
                    break;
                }

                var savedHeaders = blockchain.Add(newChain);

                foreach (DbHeader header in savedHeaders)
                {
                    knownParentsByHash[header.Hash] = header;

                    if (bestHeader == null || header.IsBetterThan(bestHeader))
                    {
                        bestHeader = header;
                    }
                }

                remainingHeaders = orphanHeaders;
            }

            logger.Debug(() =>
                $"Received headers from endpoint '{endpoint.PeerInfo.IpEndpoint}'. " +
                $"Best received header: {(bestHeader == null ? "none" : $"{{height: {bestHeader?.Height}, hash: {HexUtils.GetString(bestHeader.Hash)}}}")}."
            );

            if (bestHeader != null)
            {
                var blockLocator = GetLocator(bestHeader);
                endpoint.WriteMessage(new GetHeadersMessage(endpoint.ProtocolVersion, blockLocator, new byte[32]));
            }

            // todo: this code look misplaced
            var bestHeadAfterUpdate = blockchain.GetBestHead();
            if (bestHeadBeforeUpdate != bestHeadAfterUpdate)
            {
                eventDispatcher.Raise(new BestHeadChangedEvent());
            }
        }

        private List<DbHeader> ExtractAndValidateSingleChain(List<DbHeader> headers, Dictionary<byte[], DbHeader> knownParentsByHash, out List<DbHeader> orphanHeaders)
        {
            HeaderSubChain subChain = null;
            List<DbHeader> newChain = new List<DbHeader>();
            orphanHeaders = new List<DbHeader>();

            foreach (var header in headers)
            {
                if (subChain == null && knownParentsByHash.ContainsKey(header.ParentHash))
                {
                    subChain = blockchain.GetSubChain(header.ParentHash, BlockHeaderValidator.RequiredSubchainLength);
                }

                if (subChain == null || !ByteArrayComparer.Instance.Equals(subChain.Head.Hash, header.ParentHash))
                {
                    orphanHeaders.Add(header);
                    continue;
                }

                DbHeader parentHeader = subChain.Head;
                if (!parentHeader.IsValid)
                {
                    //todo: disconnect node
                    throw new BitcoinProtocolViolationException(
                        $"Block header '{HexUtils.GetString(header.Hash)}' is a child of an invalid header '{HexUtils.GetString(parentHeader.Hash)}'."
                    );
                }

                double work = DifficultyUtils.DifficultyTargetToWork(DifficultyUtils.NBitsToTarget(header.NBits));
                DbHeader newHeader = header.AppendAfter(parentHeader, work);

                if (!BlockHeaderValidator.IsValid(networkParameters, newHeader, subChain))
                {
                    //todo: disconnect node
                    throw new BitcoinProtocolViolationException($"Invalid block header received. Hash: '{HexUtils.GetString(newHeader.Hash)}'.");
                }

                subChain.Append(newHeader);
                newChain.Add(newHeader);
            }

            return newChain;
        }

        private List<DbHeader> CreateAndValidateDbHeaders(BlockHeader[] messageHeaders)
        {
            List<DbHeader> headers = new List<DbHeader>(messageHeaders.Length);

            foreach (var header in messageHeaders)
            {
                // todo: we would not need write method if payload were passed with event
                byte[] hash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(header.Write));

                if (!BlockHeaderValidator.IsValid(networkParameters, header, hash))
                {
                    //todo: disconnect node
                    throw new BitcoinProtocolViolationException($"Invalid block header received. Hash: '{HexUtils.GetString(hash)}'.");
                }

                headers.Add(new DbHeader(header, hash, -1, 0, true));
            }

            return headers;
        }

        private Dictionary<byte[], DbHeader> FetchKnownParents(List<DbHeader> headers)
        {
            var knownParents = blockchain.GetHeaders(headers.Select(h => h.ParentHash));
            Dictionary<byte[], DbHeader> knownParentsByHash = new Dictionary<byte[], DbHeader>(ByteArrayComparer.Instance);
            foreach (DbHeader parent in knownParents)
            {
                knownParentsByHash[parent.Hash] = parent;
            }

            return knownParentsByHash;
        }

        private byte[][] GetLocator(DbHeader knownHeader)
        {
            List<DbHeader> locatorHeaders = new List<DbHeader>();

            locatorHeaders.AddRange(blockchain.GetSubChain(knownHeader.ParentHash, 999) ?? Enumerable.Empty<DbHeader>());
            locatorHeaders.Add(knownHeader);

            DbHeader bestHead = blockchain.GetBestHead(knownHeader.Hash);
            if (bestHead != null && bestHead.Height != knownHeader.Height)
            {
                int bestRelatedChainLength = Math.Min(bestHead.Height - knownHeader.Height, 1000);
                var bestRelatedChain = blockchain.GetSubChain(bestHead.Hash, bestRelatedChainLength);
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