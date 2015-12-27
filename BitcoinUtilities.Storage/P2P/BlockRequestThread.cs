using System;
using System.Collections.Generic;
using System.Threading;
using BitcoinUtilities.Collections;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Storage.Converters;
using BitcoinUtilities.Storage.Models;

namespace BitcoinUtilities.Storage.P2P
{
    public class BlockRequestThread
    {
        private const int MaxSentRequests = 10;
        private const int MaxSendAttempts = 3;
        private static readonly TimeSpan requestTimeout = TimeSpan.FromSeconds(30);

        private volatile bool running = true;

        private readonly BlockConverter blockConverter = new BlockConverter();

        private readonly BitcoinEndpoint endpoint;

        private readonly object dataLock = new object();
        private readonly AutoResetEvent dataChangedEvent = new AutoResetEvent(false);

        private readonly LinkedDictionary<byte[], BlockRequest> unsentRequests = new LinkedDictionary<byte[], BlockRequest>(ByteArrayComparer.Instance);
        private readonly LinkedDictionary<byte[], BlockRequest> sentRequests = new LinkedDictionary<byte[], BlockRequest>(ByteArrayComparer.Instance);

        public BlockRequestThread(BitcoinEndpoint endpoint)
        {
            this.endpoint = endpoint;
        }

        public int RequestsCount
        {
            get
            {
                lock (dataLock)
                {
                    return unsentRequests.Count + sentRequests.Count;
                }
            }
        }

        public void Request(IEnumerable<byte[]> blocks)
        {
            lock (dataLock)
            {
                foreach (byte[] block in blocks)
                {
                    if (!sentRequests.ContainsKey(block) && !unsentRequests.ContainsKey(block))
                    {
                        BlockRequest request = new BlockRequest();
                        request.Hash = block;
                        request.SendDate = null;
                        unsentRequests.Add(block, request);
                    }
                }
            }

            dataChangedEvent.Set();
        }

        //todo: remove this method, it causes troubles
        public void CancelRequests()
        {
            lock (dataLock)
            {
                unsentRequests.Clear();
                //sentRequests.Clear();
            }

            dataChangedEvent.Set();
        }

        public void ProcessMessage(IBitcoinMessage message)
        {
            if (message is BlockMessage)
            {
                ProcessBlock((BlockMessage) message);
            }
        }

        public void Stop()
        {
            running = false;
            dataChangedEvent.Set();
        }

        public void MainLoop()
        {
            while (running)
            {
                RemoveObsoleteRequests();

                //todo: remove dubug output
                //Console.WriteLine("> fetching blocks to send");

                //todo: also mark requests as deprecated
                List<BlockRequest> requests = FetchBlocksToRequest();

                if (requests.Count == 0)
                {
                    //todo: remove dubug output
                    //Console.WriteLine("> awaiting changes");

                    dataChangedEvent.WaitOne(requestTimeout);
                    continue;
                }

                //todo: remove dubug output
                //Console.WriteLine("> preparing data request");


                for (int i = 0; i < requests.Count; i++)
                {
                    BlockRequest request = requests[i];
                    InventoryVector[] inventory = new InventoryVector[1];
                    //todo: BlockRequest should contain InventoryVector instead
                    inventory[0] = new InventoryVector(InventoryVectorType.MsgBlock, request.Hash);

                    lock (dataLock)
                    {
                        DateTime utcNow = DateTime.UtcNow;
                        request.SendDate = utcNow;
                        request.SendAttempts++;
                        sentRequests.Add(request.Hash, request);
                        endpoint.WriteMessage(new GetDataMessage(inventory));
                    }
                }

                //endpoint.WriteMessage(new GetDataMessage(inventory));

                //todo: remove dubug output
                //Console.WriteLine("> [BlockRequestThread] requested {0} blocks from: {1} to {2}", inventory.Length, BitConverter.ToString(inventory.First().Hash), BitConverter.ToString(inventory.Last().Hash));
            }
        }

        private void RemoveObsoleteRequests()
        {
            List<BlockRequest> requestsToResend = new List<BlockRequest>();
            DateTime utcNow = DateTime.UtcNow;
            DateTime thresholdTime = utcNow - requestTimeout;
            lock (dataLock)
            {
                List<BlockRequest> requestsToRemove = new List<BlockRequest>();
                foreach (KeyValuePair<byte[], BlockRequest> pair in sentRequests)
                {
                    BlockRequest request = pair.Value;
                    if (request.SendDate > utcNow)
                    {
                        request.SendDate = utcNow;
                        //todo: remove dubug output
                        Console.WriteLine(">> !!!!! correcting sendDate from future");
                    }
                    else if (request.SendDate < thresholdTime)
                    {
                        requestsToRemove.Add(request);
                    }
                }
                if (requestsToRemove.Count > 0)
                {
                    //todo: remove dubug output
                    Console.WriteLine(">> removed {0} requests, starting from {1}", requestsToRemove.Count, BitConverter.ToString(requestsToRemove[0].Hash));
                    foreach (BlockRequest request in requestsToRemove)
                    {
                        sentRequests.Remove(request.Hash);
                        if (request.SendAttempts < MaxSendAttempts)
                        {
                            //todo: review locks here
                            request.SendAttempts++;
                            request.SendDate = utcNow;
                            sentRequests.Add(request.Hash, request);
                            requestsToResend.Add(request);
                        }
                    }
                }
            }

            foreach (BlockRequest request in requestsToResend)
            {
                endpoint.WriteMessage(new GetDataMessage(new InventoryVector[] {new InventoryVector(InventoryVectorType.MsgBlock, request.Hash)}));
                //todo: remove dubug output
                Console.WriteLine(">> repeat block request for block {0}, attempt #{1}", BitConverter.ToString(request.Hash), request.SendAttempts);
            }
        }

        private List<BlockRequest> FetchBlocksToRequest()
        {
            List<BlockRequest> res = new List<BlockRequest>();
            lock (dataLock)
            {
                int blocksToFetch = MaxSentRequests - sentRequests.Count;
                if (blocksToFetch == 0)
                {
                    return res;
                }
                foreach (KeyValuePair<byte[], BlockRequest> pair in unsentRequests)
                {
                    if (blocksToFetch == 0)
                    {
                        break;
                    }
                    res.Add(pair.Value);
                    blocksToFetch--;
                }
                //todo: instead define TryRemoveFirst method
                foreach (BlockRequest request in res)
                {
                    unsentRequests.Remove(request.Hash);
                }
            }
            return res;
        }

        private void ProcessBlock(BlockMessage blockMessage)
        {
            Block block = blockConverter.FromMessage(blockMessage);
            byte[] blockHash = block.Hash;

            lock (dataLock)
            {
                unsentRequests.Remove(blockHash);
                sentRequests.Remove(blockHash);
            }

            dataChangedEvent.Set();
        }

        private class BlockRequest
        {
            public byte[] Hash { get; set; }
            public DateTime? SendDate { get; set; }
            public int SendAttempts { get; set; }
        }
    }
}