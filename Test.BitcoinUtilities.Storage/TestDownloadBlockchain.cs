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
using BitcoinUtilities.Storage;
using BitcoinUtilities.Storage.Converters;
using BitcoinUtilities.Storage.Models;
using BitcoinUtilities.Storage.P2P;
using NLog;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Storage
{
    [TestFixture]
    public class TestDownloadBlockchain
    {
        private const string StorageLocation = @"D:\Temp\blockchain.db";

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

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
        private Exception saveThreadError;

        private BlockChainStorage storage;

        [Test]
        [Explicit]
        public void DownloadBlocks()
        {
            Init();

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
                while (idleSeconds < 300)
                {
                    lock (dataLock)
                    {
                        if (saveThreadError != null)
                        {
                            throw new Exception("Error in save thread.", saveThreadError);
                        }
                    }

                    if (!requestBlockListEvent.WaitOne(10000))
                    {
                        idleSeconds += 10;
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
        }

        private void Init()
        {
            storage = BlockChainStorage.Open(StorageLocation);
            Block lastBlock = storage.GetLastBlockHeader();

            //todo: make this logic more reusable
            if (lastBlock != null)
            {
                //todo: use better locator
                lastSavedBlocks.Enqueue(lastBlock);
            }
            else
            {
                MemoryStream mem = new MemoryStream(GenesisBlock.Raw);
                using (BitcoinStreamReader reader = new BitcoinStreamReader(mem))
                {
                    BlockMessage blockMessage = BlockMessage.Read(reader);
                    lastSavedBlocks.Enqueue(blockConverter.FromMessage(blockMessage));
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
                    if (!knownBlocks.ContainsKey(vector.Hash))
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
                BlockMessage blockMessage = (BlockMessage)message;
                Block block = blockConverter.FromMessage(blockMessage);

                byte[] prevBlockHash = new byte[32];
                Array.Copy(blockMessage.BlockHeader.PrevBlock, prevBlockHash, 32);
                Array.Reverse(prevBlockHash);

                lock (dataLock)
                {
                    blockPool[blockMessage.BlockHeader.PrevBlock] = block;

                    //Console.WriteLine(">> received block (hash={0}, prevBlock={1})", BitConverter.ToString(block.Hash), BitConverter.ToString(prevBlockHash).Replace("-", ""));

                    if (block.Hash[31] != 0)
                    {
                        Console.WriteLine(">> suspicious block (hash={0}, prevBlock={1})", BitConverter.ToString(block.Hash), BitConverter.ToString(prevBlockHash).Replace("-", ""));
                    }

                    knownBlocks[block.Hash] = block;
                    originalBlockchainBytes += BitcoinStreamWriter.GetBytes(blockMessage.Write).Length;
                }
                saveBlocksEvent.Set();
                //requestBlockListEvent.Set();
            }
            return true;
        }

        private void SaveThreadLoop()
        {
            try
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
            catch (Exception e)
            {
                lock (dataLock)
                {
                    saveThreadError = e;
                }
            }
        }

        private void SavePoolBlocks()
        {
            List<Block> blocksToSave = new List<Block>();
            lock (dataLock)
            {
                Block currentBlock = lastSavedBlocks.Last();
                Block nextBlock;
                while (blockPool.TryRemove(currentBlock.Hash, out nextBlock))
                {
                    nextBlock.Height = currentBlock.Height + 1;
                    currentBlock = nextBlock;
                    blocksToSave.Add(nextBlock);
                }
            }
            if (blocksToSave.Any())
            {
                SaveBlocks(blocksToSave);
            }
        }

        private void SaveBlocks(List<Block> blocksToSave)
        {
            List<Block> batch = new List<Block>();
            int batchSize = 0;

            foreach (Block block in blocksToSave)
            {
                batch.Add(block);
                batchSize += block.Transactions.Sum(t => t.Inputs.Count);
                batchSize += block.Transactions.Sum(t => t.Outputs.Count);
                if (batchSize >= 10000)
                {
                    SaveBlockBatch(batch);
                    batch.Clear();
                    batchSize = 0;
                }
            }

            if (batch.Any())
            {
                SaveBlockBatch(batch);
            }
        }

        private void SaveBlockBatch(List<Block> blocksToSave)
        {
            logger.Debug(
                "Flushing batch (blocks: {0}, transactions: {1}, inputs and outputs: {2}).",
                blocksToSave.Count,
                blocksToSave.Sum(b => b.Transactions.Count),
                blocksToSave.Sum(b => b.Transactions.Sum(t => t.Inputs.Count)) + blocksToSave.Sum(b => b.Transactions.Sum(t => t.Outputs.Count))
                );

            storage.AddBlocks(blocksToSave);

            lock (dataLock)
            {
                foreach (Block block in blocksToSave)
                {
                    block.Transactions = null;
                    lastSavedBlocks.Enqueue(block);
                    if (block.Height%1000 == 0)
                    {
                        logger.Debug("Checkpoint. Height: {0}", block.Height);
                    }
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