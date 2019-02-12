using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Collections;
using BitcoinUtilities.Node.Components;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Node.Modules.Headers;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using NLog;

namespace BitcoinUtilities.Node.Modules.Blocks
{
    public partial class BlockDownloadService : EndpointEventHandlingService
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private static readonly PerformanceCounters performanceCounters = new PerformanceCounters(logger);

        private readonly BlockRequestCollection requestCollection;
        private readonly BlockRepository repository;
        private readonly BitcoinEndpoint endpoint;

        private readonly Random random = new Random();

        private readonly LinkedDictionary<byte[], DateTime> inventory = new LinkedDictionary<byte[], DateTime>(ByteArrayComparer.Instance);
        private readonly LinkedDictionary<byte[], DateTime> sentRequests = new LinkedDictionary<byte[], DateTime>(ByteArrayComparer.Instance);

        public BlockDownloadService(
            BlockRequestCollection requestCollection,
            BlockRepository repository,
            BitcoinEndpoint endpoint
        ) : base(endpoint)
        {
            this.requestCollection = requestCollection;
            this.repository = repository;
            this.endpoint = endpoint;

            On<BlockDownloadRequestedEvent>(e => CheckState());
            OnMessage<InvMessage>(ProcessInvMessage);
            OnMessage<BlockMessage>(ProcessBlockMessage);
        }

        public override void OnStart()
        {
            base.OnStart();
            CheckState();
        }

        private void CheckState()
        {
            // todo: is this method expensive of called too often?
            RemoveOutdatedEntries(inventory);
            RemoveOutdatedEntries(sentRequests);

            var pendingRequests = requestCollection.GetPendingRequests();

            int awaitableBlocksCount = pendingRequests.Count(h => sentRequests.ContainsKey(h.Hash));
            if (awaitableBlocksCount >= 10)
            {
                return;
            }

            List<DbHeader> requestableBlocks = pendingRequests.Where(h => inventory.ContainsKey(h.Hash) && !sentRequests.ContainsKey(h.Hash)).ToList();

            List<DbHeader> newRequests = new List<DbHeader>();
            if (random.Next(4) == 0)
            {
                newRequests.AddRange(requestableBlocks.Take(1));
            }
            else
            {
                newRequests.AddRange(requestableBlocks.Skip(random.Next(10)).Take(1));
            }

            newRequests.AddRange(requestableBlocks.Skip(10 + random.Next(190)).Take(1));
            newRequests.AddRange(requestableBlocks.Skip(10 + random.Next(190)).Except(newRequests).Take(1));
            int extraRequestCount = random.Next(3);
            for (int i = 0; i < extraRequestCount; i++)
            {
                newRequests.AddRange(requestableBlocks.Skip(10 + random.Next(190)).Except(newRequests).Take(1));
            }

            if (newRequests.Any())
            {
                DateTime utcNow = DateTime.UtcNow;

                InventoryVector[] inventoryVectors = newRequests.Select(b => new InventoryVector(InventoryVectorType.MsgBlock, b.Hash)).ToArray();
                endpoint.WriteMessage(new GetDataMessage(inventoryVectors));

                foreach (var header in newRequests)
                {
                    sentRequests.Add(header.Hash, utcNow);
                }

                awaitableBlocksCount += newRequests.Count;
            }

            if (awaitableBlocksCount == 0)
            {
                byte[] locator = pendingRequests.FirstOrDefault()?.ParentHash;
                if (locator != null)
                {
                    endpoint.WriteMessage(new GetBlocksMessage(endpoint.ProtocolVersion, new byte[][] {locator}, new byte[32]));
                    inventory.Clear();
                }
            }
        }

        private void ProcessInvMessage(InvMessage message)
        {
            var advertisedBlocks = message.Inventory.Where(i => i.Type == InventoryVectorType.MsgBlock).Select(i => i.Hash);
            DateTime utcNow = DateTime.UtcNow;
            foreach (byte[] hash in advertisedBlocks)
            {
                inventory.Remove(hash);
                inventory.Add(hash, utcNow);
            }

            CheckState();
        }

        private void ProcessBlockMessage(BlockMessage blockMessage)
        {
            byte[] hash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(blockMessage.BlockHeader.Write));
            if (!IsBlockValid(blockMessage, hash))
            {
                // todo: remember as bad host? 
                endpoint.Dispose();
                return;
            }

            bool isNewBlock = repository.AddBlock(hash, blockMessage);

            performanceCounters.BlockReceived(isNewBlock, blockMessage.Transactions.Length);

            inventory.Remove(hash);
            sentRequests.Remove(hash);

            CheckState();
        }

        private bool IsBlockValid(BlockMessage blockMessage, byte[] hash)
        {
            if (!BlockHeaderValidator.IsValid(blockMessage.BlockHeader, hash))
            {
                return false;
            }

            byte[] merkleRoot = MerkleTreeUtils.GetTreeRoot(blockMessage.Transactions);
            return ByteArrayComparer.Instance.Equals(blockMessage.BlockHeader.MerkleRoot, merkleRoot);
        }

        private static void RemoveOutdatedEntries(LinkedDictionary<byte[], DateTime> entries)
        {
            //todo: check constants
            DateTime outdatedEntryDate = DateTime.UtcNow.AddSeconds(-120);
            List<byte[]> outdatedEntries = entries.Where(p => p.Value <= outdatedEntryDate).Select(p => p.Key).ToList();
            foreach (byte[] hash in outdatedEntries)
            {
                entries.Remove(hash);
            }

            // todo: handle dates from future

            //todo: check constants
            while (entries.Count > 5000)
            {
                entries.Remove(entries.First().Key);
            }
        }
    }
}