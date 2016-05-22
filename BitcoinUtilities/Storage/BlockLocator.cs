using System.Collections.Generic;
using System.Linq;

namespace BitcoinUtilities.Storage
{
    /// <summary>
    /// An updatable set of block locator hashes.
    /// </summary>
    public class BlockLocator
    {
        private const int GroupCount = 10;
        private const int ItemsPerGroup = 10;
        private static readonly int[] GroupDivisors = new int[] { 1, 4, 16, 64, 256, 1024, 4096, 16384, 65536, 262144 };

        private readonly LinkedList<BlockLocatorEntry>[] groups;

        public BlockLocator()
        {
            groups = new LinkedList<BlockLocatorEntry>[GroupCount];

            for (int groupIndex = 0; groupIndex < GroupCount; groupIndex++)
            {
                groups[groupIndex] = new LinkedList<BlockLocatorEntry>();
            }
        }

        /// <summary>
        /// Determines which block hashes are required to build a locator for the given height.
        /// </summary>
        /// <param name="targetHeight">The height of the last block that should be described by the locator.</param>
        /// <returns>An array of block heights that are required to build a locator for the given height.</returns>
        public static int[] GetRequiredBlockHeights(int targetHeight)
        {
            SortedSet<int> heights = new SortedSet<int>();

            for (int groupIndex = 0; groupIndex < GroupCount; groupIndex++)
            {
                int groupDivisor = GroupDivisors[groupIndex];
                int height = targetHeight - targetHeight%groupDivisor;
                for (int i = 0; i < ItemsPerGroup && height >= 0; i++, height -= groupDivisor)
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

            for (int groupIndex = 0; groupIndex < GroupCount; groupIndex++)
            {
                int groupDivisor = GroupDivisors[groupIndex];
                if (height%groupDivisor == 0)
                {
                    LinkedList<BlockLocatorEntry> group = groups[groupIndex];
                    while (group.Count > 0 && group.First.Value.Height >= height)
                    {
                        group.RemoveFirst();
                    }
                    while (group.Count >= ItemsPerGroup)
                    {
                        group.RemoveLast();
                    }
                    group.AddFirst(entry);
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

            List<byte[]> hashes = new List<byte[]>(GroupCount*ItemsPerGroup);
            int lastHeight = groups[0].First.Value.Height + 1;

            foreach (LinkedList<BlockLocatorEntry> group in groups)
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