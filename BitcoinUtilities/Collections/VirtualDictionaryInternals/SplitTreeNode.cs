using System.Collections.Generic;

namespace BitcoinUtilities.Collections.VirtualDictionaryInternals
{
    internal class SplitTreeNode
    {
        private readonly SplitTreeNode parent;
        private readonly int childNum;
        private readonly int maskOffset;

        private int nodeIndex;
        private long blockOffset;
        private List<Record> records;

        public SplitTreeNode(SplitTreeNode parent, int childNum, int maskOffset, int nodeIndex, long blockOffset, List<Record> records)
        {
            this.parent = parent;
            this.childNum = childNum;
            this.maskOffset = maskOffset;

            this.nodeIndex = nodeIndex;
            this.blockOffset = blockOffset;
            this.records = records;
        }

        public SplitTreeNode Parent
        {
            get { return parent; }
        }

        public int ChildNum
        {
            get { return childNum; }
        }

        public int MaskOffset
        {
            get { return maskOffset; }
        }

        public int NodeIndex
        {
            get { return nodeIndex; }
            set { nodeIndex = value; }
        }

        public long BlockOffset
        {
            get { return blockOffset; }
            set { blockOffset = value; }
        }

        public List<Record> Records
        {
            get { return records; }
            set { records = value; }
        }
    }
}