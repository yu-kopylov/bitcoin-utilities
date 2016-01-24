using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace BitcoinUtilities.Collections
{
    public class VirtualDictionary : IDisposable
    {
        internal int MaxNonIndexedRecordsCount = 1000;
        internal int RecordsPerBlock = 128;

        //todo: use to accumulate small changes
        private readonly List<NonIndexedRecord> nonIndexedRecords = new List<NonIndexedRecord>();
        //todo: use local comparer
        private readonly Dictionary<byte[], int> nonIndexedRecordsByKey = new Dictionary<byte[], int>(ByteArrayComparer.Instance);

        private readonly Dictionary<long, Block> blocks = new Dictionary<long, Block>();

        private readonly CompactTree blockTree;

        private VirtualDictionary(string filename, int keySize, int valueSize)
        {
            blockTree = new CompactTree(CompactTreeNode.CreateDataNode(0));
            blocks.Add(0, new Block());
        }

        public static VirtualDictionary Open(string filename, int keySize, int valueSize)
        {
            return new VirtualDictionary(filename, keySize, valueSize);
        }

        public void Dispose()
        {
            //todo: implement
        }

        public VirtualDictionaryTransaction BeginTransaction()
        {
            return new VirtualDictionaryTransaction(this);
        }

        //todo: class or struct?
        internal class NonIndexedRecord
        {
            private readonly byte[] key;
            private readonly byte[] value;

            public NonIndexedRecord(byte[] key, byte[] value)
            {
                this.key = key;
                this.value = value;
            }

            public byte[] Key
            {
                get { return key; }
            }

            public byte[] Value
            {
                get { return value; }
            }
        }

        internal bool TryGetNonIndexedValue(byte[] key, out byte[] value)
        {
            int recordIndex;
            if (nonIndexedRecordsByKey.TryGetValue(key, out recordIndex))
            {
                value = nonIndexedRecords[recordIndex].Value;
                return true;
            }
            value = null;
            return false;
        }

        public void AddNonIndexedValue(byte[] key, byte[] value)
        {
            //todo: implement correctly (depends on record being class or struct)
            NonIndexedRecord record = new NonIndexedRecord(key, value);
            int recordIndex;
            if (nonIndexedRecordsByKey.TryGetValue(key, out recordIndex))
            {
                nonIndexedRecords[recordIndex] = record;
            }
            else
            {
                recordIndex = nonIndexedRecords.Count;
                nonIndexedRecords.Add(record);
                nonIndexedRecordsByKey.Add(key, recordIndex);
            }
        }

        internal List<NonIndexedRecord> GetNonIndexedRecords()
        {
            return nonIndexedRecords;
        }

        internal CompactTree GetBlockTree()
        {
            return blockTree;
        }

        public void ClearNonIndexedRecords()
        {
            nonIndexedRecords.Clear();
            nonIndexedRecordsByKey.Clear();
        }

        internal long CreateDataBlock(List<Record> records)
        {
            long blockOffset = blocks.Count;
            Block block = new Block();
            blocks.Add(blockOffset, block);
            block.Records = records;
            return blockOffset;
        }

        internal Block GetBlock(long blockOffset)
        {
            return blocks[blockOffset];
        }
    }

    public class VirtualDictionaryTransaction : IDisposable
    {
        private readonly VirtualDictionary dictionary;

        //todo: use local comparer
        private readonly Dictionary<byte[], byte[]> unsavedRecords = new Dictionary<byte[], byte[]>(ByteArrayComparer.Instance);

        internal VirtualDictionaryTransaction(VirtualDictionary dictionary)
        {
            this.dictionary = dictionary;
        }

        public void Dispose()
        {
            Rollback();
        }

        public void AddOrUpdate(byte[] key, byte[] value)
        {
            //todo: validate key and value length
            unsavedRecords[key] = value;
        }

        public Dictionary<byte[], byte[]> Find(List<byte[]> keys)
        {
            //todo: use local comparer
            Dictionary<byte[], byte[]> res = new Dictionary<byte[], byte[]>(ByteArrayComparer.Instance);

            List<Record> recordsToLookup = new List<Record>();

            foreach (byte[] key in keys)
            {
                byte[] value;
                if (unsavedRecords.TryGetValue(key, out value))
                {
                    res.Add(key, value);
                }
                else if (dictionary.TryGetNonIndexedValue(key, out value))
                {
                    res.Add(key, value);
                }
                else
                {
                    recordsToLookup.Add(new Record(key, null));
                }
            }

            CompactTree tree = dictionary.GetBlockTree();

            SortedDictionary<long, BlockBatch> blockBatches = SplitByBlock(tree, recordsToLookup);

            foreach (BlockBatch batch in blockBatches.Values)
            {
                Block block = dictionary.GetBlock(batch.BlockOffset);
                FindRecords(res, block.Records, batch.Records, batch.MaskOffset/8);
            }

            return res;
        }

        private void FindRecords(Dictionary<byte[], byte[]> res, List<Record> recordsA, List<Record> recordsB, int firstByte)
        {
            int ofsA = 0, ofsB = 0;

            while (ofsA < recordsA.Count && ofsB < recordsB.Count)
            {
                Record recordA = recordsA[ofsA];
                Record recordB = recordsB[ofsB];

                int diff = CompareKeys(recordA.Key, recordB.Key, firstByte);

                if (diff < 0)
                {
                    ofsA++;
                }
                else if (diff > 0)
                {
                    ofsB++;
                }
                else
                {
                    //todo: copy value to avoid rewrite by client?
                    res.Add(recordB.Key, recordA.Value);
                    ofsA++;
                    ofsB++;
                }
            }
        }

        public void Commit()
        {
            if (unsavedRecords.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<byte[], byte[]> unsavedRecord in unsavedRecords)
            {
                dictionary.AddNonIndexedValue(unsavedRecord.Key, unsavedRecord.Value);
            }

            List<VirtualDictionary.NonIndexedRecord> nonIndexedRecords = dictionary.GetNonIndexedRecords();

            if (nonIndexedRecords.Count <= dictionary.MaxNonIndexedRecordsCount)
            {
                return;
            }

            CompactTree tree = dictionary.GetBlockTree();

            AddRecordsToTree(tree, nonIndexedRecords);

            dictionary.ClearNonIndexedRecords();
        }

        private void AddRecordsToTree(CompactTree tree, List<VirtualDictionary.NonIndexedRecord> nonIndexedRecords)
        {
            SortedDictionary<long, BlockBatch> blockUpdates = SplitByBlock(tree, nonIndexedRecords.Select(r => new Record(r.Key, r.Value)));

            List<SplitTreeNode> updatedTreeNodes = new List<SplitTreeNode>();

            foreach (BlockBatch blockUpdate in blockUpdates.Values)
            {
                List<SplitTreeNode> treeNodes = UpdateBlock(blockUpdate);
                if (treeNodes != null)
                {
                    updatedTreeNodes.AddRange(treeNodes);
                }
            }

            foreach (SplitTreeNode treeNode in updatedTreeNodes)
            {
                if (treeNode.NodeIndex >= 0)
                {
                    Contract.Assert(treeNode.Records == null, "Only root of split tree has non-empty NodeIndex.");
                    tree.Nodes[treeNode.NodeIndex] = CompactTreeNode.CreateSplitNode();
                }
                else if (treeNode.NodeIndex < 0)
                {
                    if (treeNode.Records == null)
                    {
                        treeNode.NodeIndex = tree.AddSplitNode(treeNode.Parent.NodeIndex, treeNode.ChildNum);
                    }
                    else
                    {
                        long blockOffset = treeNode.BlockOffset;
                        if (blockOffset < 0)
                        {
                            blockOffset = dictionary.CreateDataBlock(treeNode.Records);
                        }
                        treeNode.NodeIndex = tree.AddDataNode(treeNode.Parent.NodeIndex, treeNode.ChildNum, blockOffset);
                    }
                }
            }
        }

        private SortedDictionary<long, BlockBatch> SplitByBlock(CompactTree tree, IEnumerable<Record> records)
        {
            SortedDictionary<long, BlockBatch> blockBatches = new SortedDictionary<long, BlockBatch>();

            foreach (var record in records)
            {
                int maskOffset = 0;
                int nodeIndex = 0;
                CompactTreeNode node = tree.Nodes[nodeIndex];
                while (!node.IsDataNode)
                {
                    bool left = (record.Key[maskOffset/8] & (1 << (7 - maskOffset%8))) == 0;
                    nodeIndex = node.GetChild(left ? 0 : 1);
                    node = tree.Nodes[nodeIndex];
                    maskOffset++;
                }

                long blockOffset = node.Value;

                BlockBatch blockBatch;
                if (!blockBatches.TryGetValue(blockOffset, out blockBatch))
                {
                    blockBatch = new BlockBatch(blockOffset, maskOffset, nodeIndex);
                    blockBatches.Add(blockOffset, blockBatch);
                }

                blockBatch.Records.Add(record);
            }

            foreach (BlockBatch batch in blockBatches.Values)
            {
                batch.Records.Sort((recordA, recordB) => CompareKeys(recordA.Key, recordB.Key, batch.MaskOffset/8));
            }

            return blockBatches;
        }

        private List<SplitTreeNode> UpdateBlock(BlockBatch blockBatch)
        {
            Block block = dictionary.GetBlock(blockBatch.BlockOffset);

            //todo: review types
            List<Record> records = MergeRecords(block.Records, blockBatch.Records, blockBatch.MaskOffset/8);

            //todo: is it worth to have this check?
            if (records.Count <= dictionary.RecordsPerBlock)
            {
                block.Records = records;
                return null;
            }

            SplitTreeNode splitTreeRoot = new SplitTreeNode(null, -1, blockBatch.MaskOffset, blockBatch.NodeIndex, blockBatch.BlockOffset, records);

            List<SplitTreeNode> updatedTreeNodes = new List<SplitTreeNode>();
            updatedTreeNodes.Add(splitTreeRoot);

            List<SplitTreeNode> unsplitTreeNodes = new List<SplitTreeNode>();
            unsplitTreeNodes.Add(splitTreeRoot);

            while (unsplitTreeNodes.Count != 0)
            {
                SplitTreeNode nodeToSplit = unsplitTreeNodes[unsplitTreeNodes.Count - 1];
                unsplitTreeNodes.RemoveAt(unsplitTreeNodes.Count - 1);

                List<SplitTreeNode> newNodes = Split(nodeToSplit);
                foreach (SplitTreeNode treeNode in newNodes)
                {
                    updatedTreeNodes.Add(treeNode);
                    if (treeNode.Records.Count > dictionary.RecordsPerBlock)
                    {
                        unsplitTreeNodes.Add(treeNode);
                    }
                }
            }

            return updatedTreeNodes;
        }

        private List<Record> MergeRecords(List<Record> recordsA, List<Record> recordsB, int firstByte)
        {
            List<Record> res = new List<Record>();

            int ofsA = 0, ofsB = 0;

            while (ofsA < recordsA.Count && ofsB < recordsB.Count)
            {
                Record recordA = recordsA[ofsA];
                Record recordB = recordsB[ofsB];

                int diff = CompareKeys(recordA.Key, recordB.Key, firstByte);

                if (diff < 0)
                {
                    res.Add(recordA);
                    ofsA++;
                }
                else if (diff > 0)
                {
                    res.Add(recordB);
                    ofsB++;
                }
                else
                {
                    res.Add(recordB);
                    ofsA++;
                    ofsB++;
                }
            }
            while (ofsA < recordsA.Count)
            {
                res.Add(recordsA[ofsA++]);
            }
            while (ofsB < recordsB.Count)
            {
                res.Add(recordsB[ofsB++]);
            }

            return res;
        }

        private int CompareKeys(byte[] keyA, byte[] keyB, int firstByte)
        {
            Contract.Assume(keyA.Length == keyB.Length, "Both keys should have the same size.");
            for (int i = firstByte; i < keyA.Length; i++)
            {
                int diff = keyA[i] - keyB[i];
                if (diff != 0)
                {
                    return diff;
                }
            }
            return 0;
        }

        private List<SplitTreeNode> Split(SplitTreeNode node)
        {
            List<Record> records = node.Records;
            node.Records = null;

            int maskByte = node.MaskOffset/8;
            byte mask = (byte) (1 << (7 - node.MaskOffset%8));

            int firstRight = 0;
            for (; firstRight < records.Count; firstRight++)
            {
                bool isLeft = (records[firstRight].Key[maskByte] & mask) == 0;
                if (!isLeft)
                {
                    break;
                }
            }

            List<SplitTreeNode> newNodes = new List<SplitTreeNode>();
            if (firstRight != 0)
            {
                SplitTreeNode newNode = new SplitTreeNode(node, 0, node.MaskOffset + 1, -1, node.BlockOffset, records.GetRange(0, firstRight));
                node.BlockOffset = -1;
                newNodes.Add(newNode);
            }
            if (firstRight < records.Count)
            {
                SplitTreeNode newNode = new SplitTreeNode(node, 1, node.MaskOffset + 1, -1, node.BlockOffset, records.GetRange(firstRight, records.Count - firstRight));
                node.BlockOffset = -1;
                newNodes.Add(newNode);
            }

            return newNodes;
        }

        public void Rollback()
        {
            //todo: inplement
        }
    }

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

    internal class Block
    {
        private List<Record> records = new List<Record>();

        public List<Record> Records
        {
            get { return records; }
            set { records = value; }
        }
    }

    internal class Record
    {
        private readonly byte[] key;
        private readonly byte[] value;

        public Record(byte[] key, byte[] value)
        {
            this.key = key;
            this.value = value;
        }

        public byte[] Key
        {
            get { return key; }
        }

        public byte[] Value
        {
            get { return value; }
        }
    }

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