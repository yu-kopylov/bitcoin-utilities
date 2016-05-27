using System;
using System.Collections.Generic;
using System.Linq;

namespace BitcoinUtilities.Storage
{
    public class Subchain
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

        public void Append(StoredBlock block)
        {
            //todo: add XMLDOC
            StoredBlock lastBlock = blocks.Last();
            if (!lastBlock.Hash.SequenceEqual(block.Header.PrevBlock))
            {
                throw new ArgumentException($"Added block should reference last block in the {nameof(Subchain)}.", nameof(block));
            }
            blocks.Add(block);
        }

        public void Truncate(int minLength, int maxLength)
        {
            //todo: use this method?
            if (minLength > maxLength)
            {
                throw new ArgumentException($"{nameof(minLength)} should not exceed {nameof(maxLength)}.");
            }
            if (blocks.Count > maxLength)
            {
                blocks.RemoveRange(0, blocks.Count - minLength);
            }
        }

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