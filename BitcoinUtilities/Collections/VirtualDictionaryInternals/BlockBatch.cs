using System.Collections.Generic;

namespace BitcoinUtilities.Collections.VirtualDictionaryInternals
{
    internal class BlockBatch
    {
        private readonly long blockOffset;
        private readonly int maskOffset;
        private readonly int nodeIndex;
        private readonly List<Record> records = new List<Record>();

        public BlockBatch(long blockOffset, int maskOffset, int nodeIndex)
        {
            this.blockOffset = blockOffset;
            this.maskOffset = maskOffset;
            this.nodeIndex = nodeIndex;
        }

        public long BlockOffset
        {
            get { return blockOffset; }
        }

        public int MaskOffset
        {
            get { return maskOffset; }
        }

        public int NodeIndex
        {
            get { return nodeIndex; }
        }

        public List<Record> Records
        {
            get { return records; }
        }
    }
}