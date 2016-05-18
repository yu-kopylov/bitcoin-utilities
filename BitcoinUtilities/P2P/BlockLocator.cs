using System;
using System.Collections.Generic;
using System.Linq;

namespace BitcoinUtilities.P2P
{
    /// <summary>
    /// An updatable set of block locator hashes.
    /// </summary>
    public class BlockLocator
    {
        private const int DefaultItemsPerGroup = 10;
        private static readonly int[] DefaultGroupDivisors = new int[] {1, 4, 16, 64, 256, 1024, 4096, 16384, 65536, 262144};

        private readonly int groupCount;
        private readonly int itemsPerGroup;
        private readonly int[] groupDivisors;
        private readonly List<BlockLocatorEntry>[] groups;

        internal BlockLocator(int itemsPerGroup, int[] groupDivisors)
        {
            if (groupDivisors.Length == 0)
            {
                throw new ArgumentException($"The {nameof(groupDivisors)} array should contain at least one element.", nameof(groupDivisors));
            }

            if (groupDivisors[0] != 1)
            {
                throw new ArgumentException($"The first element of the {nameof(groupDivisors)} array must be equal to 1.", nameof(groupDivisors));
            }

            this.itemsPerGroup = itemsPerGroup;
            this.groupDivisors = groupDivisors;

            groupCount = groupDivisors.Length;
            groups = new List<BlockLocatorEntry>[groupCount];

            for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
            {
                groups[groupIndex] = new List<BlockLocatorEntry>();
            }
        }

        public BlockLocator() : this(DefaultItemsPerGroup, DefaultGroupDivisors)
        {
        }

        /// <summary>
        /// Determines which block hashes are required to build a locator for the given height.
        /// </summary>
        /// <param name="targetHeight">The height of the last block that should be described by the locator.</param>
        /// <returns>An array of block heights that are required to build a locator for the given height.</returns>
        public int[] GetRequiredBlockHeights(int targetHeight)
        {
            SortedSet<int> heights = new SortedSet<int>();

            for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
            {
                int groupDivisor = groupDivisors[groupIndex];
                int height = targetHeight - targetHeight%groupDivisor;
                for (int i = 0; i < itemsPerGroup && height > 0; i++, height -= groupDivisor)
                {
                    heights.Add(height);
                }
            }

            return heights.ToArray();
        }

        /// <summary>
        /// Adds a block header hash to the locator.
        /// <para/>
        /// If the provided height is less then the last known height, then the locator will be truncated.
        /// </summary>
        /// <param name="height">The height of the block.</param>
        /// <param name="hash">The hash of the block.</param>
        public void AddHash(int height, byte[] hash)
        {
            BlockLocatorEntry entry = new BlockLocatorEntry(height, hash);

            for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
            {
                int groupDivisor = groupDivisors[groupIndex];
                if (height%groupDivisor == 0)
                {
                    //todo: use circular buffer
                    while (groups[groupIndex].Count > 0 && groups[groupIndex][0].Height >= height)
                    {
                        groups[groupIndex].RemoveAt(0);
                    }
                    while (groups[groupIndex].Count >= itemsPerGroup)
                    {
                        groups[groupIndex].RemoveAt(groups[groupIndex].Count - 1);
                    }
                    groups[groupIndex].Insert(0, entry);
                }
            }
        }

        /// <summary>
        /// Builds an array of the block header hashes in reverse chronological order.
        /// </summary>
        /// <returns>An array of hashes.</returns>
        public byte[][] GetHashes()
        {
            if (groups[0].Count == 0)
            {
                return new byte[0][];
            }

            List<byte[]> hashes = new List<byte[]>(groupCount*itemsPerGroup);
            int lastHeight = groups[0][0].Height + 1;

            foreach (List<BlockLocatorEntry> group in groups)
            {
                foreach (BlockLocatorEntry entry in group)
                {
                    if (entry.Height < lastHeight)
                    {
                        hashes.Add(entry.Hash);
                        lastHeight = entry.Height;
                    }
                }
            }

            return hashes.ToArray();
        }

        private struct BlockLocatorEntry
        {
            private readonly int height;
            private readonly byte[] hash;

            public BlockLocatorEntry(int height, byte[] hash)
            {
                this.height = height;
                this.hash = hash;
            }

            public int Height
            {
                get { return height; }
            }

            public byte[] Hash
            {
                get { return hash; }
            }
        }
    }
}