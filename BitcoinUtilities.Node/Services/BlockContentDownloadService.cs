using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BitcoinUtilities.Collections;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Storage;

namespace BitcoinUtilities.Node.Services
{
    public class BlockContentDownloadService : INodeService
    {
        private const int MaxRequiredBlocks = 1024;
        private const int MaxAdvertisedBlocksPerNode = 1024;
        private const int MaxConcurrentBlockRequestsPerNode = 50;
        private const int MaxConcurrentBlockRequestsPerBlock = 2;

        private readonly CancellationToken cancellationToken;

        private readonly object lockObject = new object();

        private readonly Blockchain blockchain;

        private readonly Dictionary<BitcoinEndpoint, EndpointState> endpoints = new Dictionary<BitcoinEndpoint, EndpointState>();

        private readonly Dictionary<byte[], BlockState> requiredBlocks = new Dictionary<byte[], BlockState>(ByteArrayComparer.Instance);

        public BlockContentDownloadService(Blockchain blockchain, CancellationToken cancellationToken)
        {
            this.blockchain = blockchain;
            this.cancellationToken = cancellationToken;

            UpdateRequiredBlocks();
        }

        public void Run()
        {
            while (!cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)))
            {
                //todo: resend timeouted request
            }
        }

        public void OnNodeConnected(BitcoinEndpoint endpoint)
        {
            BlockLocator locator = null;

            lock (lockObject)
            {
                endpoints.Add(endpoint, new EndpointState(endpoint));
                if (requiredBlocks.Any())
                {
                    int minRequiredBlockHeight = requiredBlocks.Values.Min(b => b.Height);
                    int locatorHeight = minRequiredBlockHeight - 1;
                    List<StoredBlock> parentBlocks = blockchain.GetBlocksByHeight(BlockLocator.GetRequiredBlockHeights(locatorHeight));
                    locator = new BlockLocator();
                    foreach (StoredBlock block in parentBlocks)
                    {
                        locator.AddHash(block.Height, block.Hash);
                    }
                }
            }

            if (locator != null)
            {
                endpoint.WriteMessage(new GetBlocksMessage(endpoint.ProtocolVersion, locator.GetHashes(), new byte[32]));
            }
        }

        public void OnNodeDisconnected(BitcoinEndpoint endpoint)
        {
            lock (lockObject)
            {
                EndpointState endpointState;
                if (!endpoints.TryGetValue(endpoint, out endpointState))
                {
                    return;
                }

                endpoints.Remove(endpoint);
                foreach (BlockRequest blockRequest in endpointState.BlockRequests.Values)
                {
                    BlockState blockState = blockRequest.BlockState;
                    blockState.Requests.RemoveAll(r => r.Endpoint == endpoint);
                }
            }
        }

        public void ProcessMessage(BitcoinEndpoint endpoint, IBitcoinMessage message)
        {
            EndpointState endpointState;

            lock (lockObject)
            {
                if (!endpoints.TryGetValue(endpoint, out endpointState))
                {
                    //todo: if node still sends us a required block it should probably be saved
                    // endpoint is either disconnected or not not fit for this service 
                    return;
                }
            }

            var invMessage = message as InvMessage;
            if (invMessage != null)
            {
                ProcessInvMessage(endpointState, invMessage);
            }

            var blockMessage = message as BlockMessage;
            if (blockMessage != null)
            {
                ProcessBlockMessage(blockMessage);
            }
        }

        private void ProcessInvMessage(EndpointState endpointState, InvMessage invMessage)
        {
            DateTime utcNow = SystemTime.UtcNow;

            GetDataMessage getDataMessage = null;

            lock (lockObject)
            {
                foreach (InventoryVector vector in invMessage.Inventory)
                {
                    if (vector.Type == InventoryVectorType.MsgBlock)
                    {
                        endpointState.AdvertisedBlocks.Add(vector.Hash, utcNow);
                    }
                }

                while (endpointState.AdvertisedBlocks.Count > MaxAdvertisedBlocksPerNode)
                {
                    endpointState.AdvertisedBlocks.Remove(endpointState.AdvertisedBlocks.First().Key);
                }

                int maxBlocksToRequest = MaxConcurrentBlockRequestsPerNode - endpointState.BlockRequests.Count;
                List<BlockState> blocksToRequest = new List<BlockState>();

                if (maxBlocksToRequest > 0)
                {
                    foreach (BlockState blockState in requiredBlocks.Values)
                    {
                        if (blockState.GetPendingRequestCount() >= MaxConcurrentBlockRequestsPerBlock)
                        {
                            continue;
                        }

                        if (!endpointState.AdvertisedBlocks.ContainsKey(blockState.Hash))
                        {
                            continue;
                        }

                        //todo: there are situations when request was sent, but timed out and should be requested again
                        if (endpointState.HasPendingRequest(blockState.Hash))
                        {
                            continue;
                        }

                        blocksToRequest.Add(blockState);
                        if (blocksToRequest.Count >= maxBlocksToRequest)
                        {
                            break;
                        }
                    }
                }

                if (blocksToRequest.Any())
                {
                    InventoryVector[] inventoryVectors = blocksToRequest.Select(b => new InventoryVector(InventoryVectorType.MsgBlock, b.Hash)).ToArray();
                    getDataMessage = new GetDataMessage(inventoryVectors);

                    foreach (BlockState blockState in blocksToRequest)
                    {
                        BlockRequest blockRequest = new BlockRequest(blockState, endpointState.Endpoint, utcNow);
                        endpointState.BlockRequests.Add(blockState.Hash, blockRequest);
                        blockState.Requests.Add(blockRequest);
                    }
                }
            }

            if (getDataMessage != null)
            {
                endpointState.Endpoint.WriteMessage(getDataMessage);
            }
        }

        private void ProcessBlockMessage(BlockMessage blockMessage)
        {
            byte[] blockHash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(blockMessage.BlockHeader.Write));
            lock (lockObject)
            {
                if (!requiredBlocks.ContainsKey(blockHash))
                {
                    return;
                }

                //todo: here there is a lock within lock
                //todo: if block is invalid don't ask for it again
                //todo: if block is invalid truncate headers chain and select other chain if necessary
                blockchain.AddBlockContent(blockMessage);
                RemoveRequiredBlock(blockHash);
            }
        }

        private void UpdateRequiredBlocks()
        {
            lock (lockObject)
            {
                List<StoredBlock> blocks = blockchain.GetOldestBlocksWithoutContent(MaxRequiredBlocks);
                Dictionary<byte[], StoredBlock> blocksMap = blocks.ToDictionary(b => b.Hash, ByteArrayComparer.Instance);

                foreach (byte[] hash in requiredBlocks.Keys)
                {
                    if (!blocksMap.ContainsKey(hash))
                    {
                        RemoveRequiredBlock(hash);
                    }
                }

                foreach (StoredBlock block in blocks)
                {
                    if (!requiredBlocks.ContainsKey(block.Hash))
                    {
                        requiredBlocks.Add(block.Hash, new BlockState(block.Height, block.Hash));
                    }
                }
            }
        }

        private void RemoveRequiredBlock(byte[] blockHash)
        {
            BlockState blockState;
            if (!requiredBlocks.TryGetValue(blockHash, out blockState))
            {
                return;
            }

            foreach (BlockRequest request in blockState.Requests)
            {
                BitcoinEndpoint endpoint = request.Endpoint;
                EndpointState endpointState = endpoints[endpoint];
                endpointState.BlockRequests.Remove(blockHash);
            }
        }

        private class EndpointState
        {
            public EndpointState(BitcoinEndpoint endpoint)
            {
                Endpoint = endpoint;
                AdvertisedBlocks = new LinkedDictionary<byte[], DateTime>(ByteArrayComparer.Instance);
                BlockRequests = new Dictionary<byte[], BlockRequest>(ByteArrayComparer.Instance);
            }

            public BitcoinEndpoint Endpoint { get; private set; }

            public LinkedDictionary<byte[], DateTime> AdvertisedBlocks { get; private set; }

            public Dictionary<byte[], BlockRequest> BlockRequests { get; private set; }

            public bool HasPendingRequest(byte[] key)
            {
                //todo: use timeout
                return BlockRequests.ContainsKey(key);
            }
        }

        private class BlockState
        {
            public BlockState(int height, byte[] hash)
            {
                Height = height;
                Hash = hash;
                Requests = new List<BlockRequest>();
            }

            public int Height { get; private set; }

            public byte[] Hash { get; private set; }

            public List<BlockRequest> Requests { get; private set; }

            public int GetPendingRequestCount()
            {
                //todo: use timeout
                return Requests.Count;
            }
        }

        private class BlockRequest
        {
            public BlockRequest(BlockState blockState, BitcoinEndpoint endpoint, DateTime requestDate)
            {
                this.BlockState = blockState;
                this.Endpoint = endpoint;
                this.RequestDate = requestDate;
            }

            public BlockState BlockState { get; private set; }

            public BitcoinEndpoint Endpoint { get; private set; }

            public DateTime RequestDate { get; private set; }
        }
    }
}