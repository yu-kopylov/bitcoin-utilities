using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Collections;
using BitcoinUtilities.Node.Modules.Headers;
using BitcoinUtilities.P2P.Messages;

namespace BitcoinUtilities.Node.Modules.Blocks
{
    public class BlockRepository
    {
        private const int MaxCachedBlocks = 3000;

        private readonly object monitor = new object();

        private readonly LinkedDictionary<byte[], BlockMessage> blocks = new LinkedDictionary<byte[], BlockMessage>(ByteArrayComparer.Instance);

        public BlockMessage GetBlock(byte[] hash)
        {
            lock (monitor)
            {
                blocks.TryGetValue(hash, out var block);
                return block;
            }
        }

        public IReadOnlyList<DbHeader> PrefetchBlocks(IReadOnlyList<DbHeader> headers)
        {
            List<DbHeader> missingBlocks = new List<DbHeader>();

            lock (monitor)
            {
                foreach (DbHeader header in headers)
                {
                    if (blocks.TryGetValue(header.Hash, out var existingBlock))
                    {
                        blocks.Remove(header.Hash);
                        blocks.Add(header.Hash, existingBlock);
                    }
                    else
                    {
                        missingBlocks.Add(header);
                    }
                }
            }

            return missingBlocks;
        }

        public bool AddBlock(byte[] hash, BlockMessage block)
        {
            lock (monitor)
            {
                bool isNewBlock = !blocks.ContainsKey(hash);
                if (isNewBlock)
                {
                    blocks[hash] = block;
                }

                while (blocks.Count > MaxCachedBlocks)
                {
                    blocks.Remove(blocks.First().Key);
                }

                return isNewBlock;
            }
        }
    }
}