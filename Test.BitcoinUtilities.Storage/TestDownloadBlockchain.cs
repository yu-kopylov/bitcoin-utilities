using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BitcoinUtilities;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Storage.Converters;
using BitcoinUtilities.Storage.Models;
using BitcoinUtilities.Storage.P2P;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Storage
{
    [TestFixture]
    public class TestDownloadBlockchain
    {
        private readonly BlockConverter blockConverter = new BlockConverter();
        private readonly ConcurrentDictionary<byte[], Block> knownBlocks = new ConcurrentDictionary<byte[], Block>(ByteArrayComparer.Instance);
        private readonly ConcurrentDictionary<byte[], Block> blockPool = new ConcurrentDictionary<byte[], Block>(ByteArrayComparer.Instance);

        //todo: split to more granular locks
        private readonly object dataLock = new object();

        private volatile Queue<Block> lastSavedBlocks = new Queue<Block>();
        private volatile byte[] lastRequestedBlockHash;

        private long originalBlockchainBytes;

        private readonly AutoResetEvent requestBlockListEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent saveBlocksEvent = new AutoResetEvent(false);

        private BlockRequestThread blockRequestThread;

        [Test]
        [Explicit]
        public void DownloadBlocks()
        {
            MemoryStream mem = new MemoryStream(GenesisBlock.Raw);
            using (BitcoinStreamReader reader = new BitcoinStreamReader(mem))
            {
                BlockMessage blockMessage = BlockMessage.Read(reader);
                lastSavedBlocks.Enqueue(blockConverter.FromMessage(blockMessage));
            }

            Thread saveThread = new Thread(SaveThreadLoop);
            saveThread.IsBackground = true;
            saveThread.Start();

            using (BitcoinEndpoint endpoint = new BitcoinEndpoint(MessageHandler))
            {
                endpoint.Connect("localhost", 8333);

                blockRequestThread = new BlockRequestThread(endpoint);

                {
                    Thread thread = new Thread(blockRequestThread.MainLoop);
                    thread.IsBackground = true;
                    thread.Start();
                }

                originalBlockchainBytes = 0;
                lastRequestedBlockHash = new byte[32];
                requestBlockListEvent.Set();

                int idleSeconds = 0;
                while (idleSeconds < 60)// && lastSavedBlock.Height < 30000)
                {
                    if (!requestBlockListEvent.WaitOne(5000))
                    {
                        idleSeconds += 5;
                    }
                    byte[][] hashes = lastSavedBlocks.Select(b => b.Hash).ToArray();
                    //todo: suboptimal
                    Block lastBlock = lastSavedBlocks.Last();
                    byte[] lastHash = lastBlock.Hash;
                    int lastHeight = lastBlock.Height;
                    if (blockRequestThread.RequestsCount < 50)
                    {
                        if (!lastHash.SequenceEqual(lastRequestedBlockHash))
                        {
                            idleSeconds = 0;
                        }
                        //blockRequestThread.CancelRequests();
                        endpoint.WriteMessage(new GetBlocksMessage(endpoint.ProtocolVersion, hashes, new byte[32]));
                        lastRequestedBlockHash = lastHash;
                        Console.WriteLine("> requested block list starting from: {0} (height: {1})", BitConverter.ToString(lastHash), lastHeight);
                    }
                }

                blockRequestThread.Stop();
                Thread.Sleep(100);
            }

            {
                Block lastBlock = lastSavedBlocks.Last();
                Console.WriteLine("> {0} bytes of block chain processed, height = {1}", originalBlockchainBytes, lastBlock.Height);
            }

            using (StreamWriter writer = new StreamWriter(@"D:\Temp\blockchain.txt"))
            {
                foreach (Block block in knownBlocks.Values.Where(b => b.Height > 0).OrderBy(b => b.Height))
                {
                    writer.WriteLine("{0:D6}:\t{1}", block.Height, BitConverter.ToString(block.Hash));
                }
            }
        }

        private bool MessageHandler(BitcoinEndpoint endpoint, IBitcoinMessage message)
        {
            //Console.WriteLine(">> received: " + message.Command);

            blockRequestThread.ProcessMessage(message);

            if (message is InvMessage)
            {
                InvMessage invMessage = (InvMessage) message;
                List<byte[]> blocksToRequest = new List<byte[]>();
                Console.WriteLine("> got invite ({0} entries)", invMessage.Inventory.Count);
                foreach (InventoryVector vector in invMessage.Inventory.Where(v => v.Type == InventoryVectorType.MsgBlock))
                {
                    //if (!knownBlocks.ContainsKey(vector.Hash))
                    {
                        blocksToRequest.Add(vector.Hash);
                    }
                }
                if (blocksToRequest.Any())
                {
                    blockRequestThread.Request(blocksToRequest);
                    Console.WriteLine("> requested {0} blocks from: {1} to {2}", blocksToRequest.Count, BitConverter.ToString(blocksToRequest.First()), BitConverter.ToString(blocksToRequest.Last()));
                }
            }
            if (message is BlockMessage)
            {
                BlockMessage blockMessage = (BlockMessage) message;
                Block block = blockConverter.FromMessage(blockMessage);
                blockPool[blockMessage.BlockHeader.PrevBlock] = block;

                byte[] prevBlockHash = new byte[32];
                Array.Copy(blockMessage.BlockHeader.PrevBlock, prevBlockHash, 32);
                Array.Reverse(prevBlockHash);

                //Console.WriteLine(">> received block (hash={0}, prevBlock={1})", BitConverter.ToString(block.Hash), BitConverter.ToString(prevBlockHash).Replace("-", ""));

                if (block.Hash[31] != 0)
                {
                    Console.WriteLine(">> suspicious block (hash={0}, prevBlock={1})", BitConverter.ToString(block.Hash), BitConverter.ToString(prevBlockHash).Replace("-", ""));
                }

                knownBlocks[block.Hash] = block;
                lock (dataLock)
                {
                    originalBlockchainBytes += BitcoinStreamWriter.GetBytes(blockMessage.Write).Length;
                }
                saveBlocksEvent.Set();
                //requestBlockListEvent.Set();
            }
            return true;
        }

        private void SaveThreadLoop()
        {
            while (true)
            {
                bool saveRequested = saveBlocksEvent.WaitOne(5000);
                if (saveRequested)
                {
                    //wait a little for other incoming blocks
                    Thread.Sleep(100);
                    SavePoolBlocks();
                }
            }
        }
        
        private void SavePoolBlocks()
        {
            List<Block> blocksToSave = new List<Block>();
            Block currentBlock = lastSavedBlocks.Last();
            Block nextBlock;
            while (blockPool.TryRemove(currentBlock.Hash, out nextBlock))
            {
                nextBlock.Height = currentBlock.Height + 1;
                currentBlock = nextBlock;
                blocksToSave.Add(nextBlock);
            }
            if (blocksToSave.Any())
            {
                //Console.WriteLine("> saving {0} blocks from: {1} to {2} (from {3} to {4})", blocksToSave.Count, blocksToSave.First().Height, blocksToSave.Last().Height, BitConverter.ToString(blocksToSave.First().Hash), BitConverter.ToString(blocksToSave.Last().Hash));
                lock (dataLock)
                {
                    foreach (Block block in blocksToSave)
                    {
                        block.Transactions = null;
                        lastSavedBlocks.Enqueue(block);
                    }
                    while (lastSavedBlocks.Count > 12)
                    {
                        lastSavedBlocks.Dequeue();
                    }
                }
                requestBlockListEvent.Set();
            }
        }
    }
}