using System.Diagnostics.Contracts;

namespace BitcoinUtilities.Collections
{
    public struct CompactTreeNode
    {
        private const ulong SplitNodeFlag = 1UL << 63;
        private const ulong ValueMask = ~(1UL << 63);

        private readonly ulong rawData;

        public CompactTreeNode(ulong rawData)
        {
            this.rawData = rawData;
        }

        public ulong RawData
        {
            get { return rawData; }
        }

        public bool IsDataNode
        {
            get { return (rawData & SplitNodeFlag) == 0; }
        }

        public bool IsSplitNode
        {
            get { return (rawData & SplitNodeFlag) != 0; }
        }

        public long Value
        {
            get { return (long)(rawData & ValueMask); }
        }

        public static CompactTreeNode CreateDataNode(long value)
        {
            Contract.Assert(value >= 0);
            return new CompactTreeNode((ulong)value);
        }

        public static CompactTreeNode CreateSplitNode()
        {
            return new CompactTreeNode(SplitNodeFlag);
        }

        private static CompactTreeNode CreateSplitNode(int childIndex0, int childIndex1)
        {
            Contract.Assert(childIndex0 >= 0);
            Contract.Assert(childIndex1 >= 0);
            ulong rawData = SplitNodeFlag | (ulong)(uint)childIndex0 << 32 | (uint)childIndex1;
            return new CompactTreeNode(rawData);
        }

        public int GetChild(int childNum)
        {
            Contract.Assert(childNum == 0 || childNum == 1);
            if (childNum == 0)
            {
                return (int)((rawData & ValueMask) >> 32);
            }
            return (int)rawData;
        }

        public CompactTreeNode SetChild(int childNum, int childIndex)
        {
            //todo: contract or specific exception?
            Contract.Assert(IsSplitNode);
            Contract.Assert(childNum == 0 || childNum == 1);
            if (childNum == 0)
            {
                //todo: use mask instead of GetChild + CreateSplitNode ?
                return CreateSplitNode(childIndex, GetChild(1));
            }
            return CreateSplitNode(GetChild(0), childIndex);
        }
    }
}