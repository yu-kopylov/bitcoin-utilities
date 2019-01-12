using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node.Rules;

namespace BitcoinUtilities.Storage
{
    public class Subchain : ISubchain<StoredBlock>
    {
        private readonly List<StoredBlock> blocks;

        public Subchain(IEnumerable<StoredBlock> blocks)
        {
            this.blocks = new List<StoredBlock>(blocks);
            for (int i = 1; i < this.blocks.Count; i++)
            {
                StoredBlock thisblock = this.blocks[i];
                StoredBlock prevBlock = this.blocks[i - 1];
                if (!prevBlock.Hash.SequenceEqual(thisblock.Header.PrevBlock))
                {
                    throw new ArgumentException($"Block #{i} is not a child of a previous block.", nameof(blocks));
                }
            }
        }

        public int Count => blocks.Count;

        public StoredBlock GetBlockByHeight(int height)
        {
            int blockIndex = height - blocks[0].Height;
            return blocks[blockIndex];
        }

        public StoredBlock GetBlockByOffset(int offset)
        {
            int blockIndex = blocks.Count - 1 - offset;
            return blocks[blockIndex];
        }
    }
}