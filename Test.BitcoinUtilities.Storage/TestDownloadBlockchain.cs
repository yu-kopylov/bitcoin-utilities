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
using NUnit.Framework;

namespace Test.BitcoinUtilities.Storage
{
    [TestFixture]
    public class TestDownloadBlockchain
    {
        private readonly BlockConverter blockConverter = new BlockConverter();
        private readonly ConcurrentDictionary<byte[], Block> knownBlocks = new ConcurrentDictionary<byte[], Block>(ByteArrayComparer.Instance);
        private readonly ConcurrentDictionary<byte[], bool> requestedBlocks = new ConcurrentDictionary<byte[], bool>(ByteArrayComparer.Instance);
        private readonly ConcurrentDictionary<byte[], Block> blockPool = new ConcurrentDictionary<byte[], Block>(ByteArrayComparer.Instance);

        private volatile Block lastSavedBlock;
        private volatile byte[] lastRequestedBlockHash;

        private long originalBlockchainBytes;

        private readonly AutoResetEvent requestBlockListEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent saveBlocksEvent = new AutoResetEvent(false);

        [Test]
        [Explicit]
        public void DownloadBlocks()
        {
            MemoryStream mem = new MemoryStream(GenesisBlock.Raw);
            using (BitcoinStreamReader reader = new BitcoinStreamReader(mem))
            {
                BlockMessage blockMessage = BlockMessage.Read(reader);
                lastSavedBlock = blockConverter.FromMessage(blockMessage);
            }

            Thread saveThread = new Thread(SaveThreadLoop);
            saveThread.IsBackground = true;
            saveThread.Start();

            using (BitcoinEndpoint endpoint = new BitcoinEndpoint(MessageHandler))
            {
                endpoint.Connect("localhost", 8333);

                originalBlockchainBytes = 0;
                lastRequestedBlockHash = new byte[32];
                requestBlockListEvent.Set();

                int idleSeconds = 0;
                while (idleSeconds < 60 && lastSavedBlock.Height < 10000)
                {
                    if (!requestBlockListEvent.WaitOne(1000))
                    {
                        idleSeconds++;
                    }
                    byte[] hash = lastSavedBlock.Hash;
                    if (!hash.SequenceEqual(lastRequestedBlockHash) && requestedBlocks.Count < 400)
                    {
                        idleSeconds = 0;
                        endpoint.WriteMessage(new GetBlocksMessage(endpoint.ProtocolVersion, new byte[][] {hash}, new byte[32]));
                        lastRequestedBlockHash = hash;
                        Console.WriteLine("> requested block list starting from: {0}", BitConverter.ToString(hash));
                    }
                }
            }
            Console.WriteLine("> {0} bytes of block chain processed, height = {1}", originalBlockchainBytes, lastSavedBlock.Height);
        }

        private bool MessageHandler(BitcoinEndpoint endpoint, IBitcoinMessage message)
        {
            if (message is InvMessage)
            {
                InvMessage invMessage = (InvMessage) message;
                List<byte[]> blocksToRequest = new List<byte[]>();
                foreach (InventoryVector vector in invMessage.Inventory.Where(v => v.Type == InventoryVectorType.MsgBlock))
                {
                    if (!knownBlocks.ContainsKey(vector.Hash))
                    {
                        blocksToRequest.Add(vector.Hash);
                        requestedBlocks[vector.Hash] = true;
                    }
                }
                if (blocksToRequest.Any())
                {
                    InventoryVector[] inventory = blocksToRequest.Select(h => new InventoryVector(InventoryVectorType.MsgBlock, h)).ToArray();
                    endpoint.WriteMessage(new GetDataMessage(inventory));
                    Console.WriteLine("> requested {0} blocks from: {1} to {2}", blocksToRequest.Count, BitConverter.ToString(blocksToRequest.First()), BitConverter.ToString(blocksToRequest.Last()));
                }
            }
            if (message is BlockMessage)
            {
                BlockMessage blockMessage = (BlockMessage) message;
                Block block = blockConverter.FromMessage(blockMessage);
                blockPool[blockMessage.BlockHeader.PrevBlock] = block;
                knownBlocks[block.Hash] = block;
                bool tmp;
                requestedBlocks.TryRemove(block.Hash, out tmp);
                originalBlockchainBytes += BitcoinStreamWriter.GetBytes(blockMessage.Write).Length;
                saveBlocksEvent.Set();
                //requestBlockListEvent.Set();
            }
            return true;
        }

        private void SaveThreadLoop()
        {
            while (true)
            {
                bool saveRequested = saveBlocksEvent.WaitOne(1000);
                if (!saveRequested || blockPool.Count > 400)
                {
                    SavePoolBlocks();
                }
            }
        }
        
        private void SavePoolBlocks()
        {
            List<Block> blocksToSave = new List<Block>();
            Block currentBlock = lastSavedBlock;
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
                foreach (Block block in blocksToSave)
                {
                    block.Transactions = null;
                }
                lastSavedBlock = blocksToSave.Last();
                requestBlockListEvent.Set();
            }
        }
    }
}