using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using BitcoinUtilities.Collections.VirtualDictionaryInternals;

namespace BitcoinUtilities.Collections
{
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
                else if (dictionary.Container.TryGetNonIndexedValue(key, out value))
                {
                    res.Add(key, value);
                }
                else
                {
                    recordsToLookup.Add(new Record(key, null));
                }
            }

            SortedDictionary<long, BlockBatch> blockBatches = SplitByBlock(recordsToLookup, false);

            foreach (BlockBatch batch in blockBatches.Values)
            {
                Block block = dictionary.Container.ReadBlock(batch.BlockOffset);
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

            List<Record> recordsToIndex = new List<Record>();

            //todo: skip records indexing if number of unsavedRecord is big enough (still need to merge them with non-indexed-values)

            foreach (KeyValuePair<byte[], byte[]> unsavedRecord in unsavedRecords)
            {
                if (!dictionary.Container.TryAddNonIndexedValue(unsavedRecord.Key, unsavedRecord.Value))
                {
                    recordsToIndex.Add(new Record(unsavedRecord.Key, unsavedRecord.Value));
                }
            }

            if (recordsToIndex.Count != 0)
            {
                recordsToIndex.AddRange(dictionary.Container.ClearNonIndexedValues());
                AddRecordsToTree(recordsToIndex);
            }

            dictionary.Container.Commit();
        }

        private void AddRecordsToTree(List<Record> nonIndexedRecords)
        {
            SortedDictionary<long, BlockBatch> blockUpdates = SplitByBlock(nonIndexedRecords, true);

            List<SplitTreeNode> updatedTreeNodes = new List<SplitTreeNode>();

            foreach (BlockBatch blockUpdate in blockUpdates.Values)
            {
                List<SplitTreeNode> treeNodes = UpdateBlock(blockUpdate);
                if (treeNodes != null)
                {
                    updatedTreeNodes.AddRange(treeNodes);
                }
            }

            //todo: specify type
            List<Tuple<long, List<Record>>> newBlocks = new List<Tuple<long, List<Record>>>();

            foreach (SplitTreeNode treeNode in updatedTreeNodes)
            {
                if (treeNode.NodeIndex >= 0)
                {
                    Contract.Assert(treeNode.Records == null, "Only root of split tree has non-empty NodeIndex.");
                    dictionary.Container.SplitDataNode(treeNode.NodeIndex);
                }
                else if (treeNode.NodeIndex < 0)
                {
                    if (treeNode.Records == null)
                    {
                        treeNode.NodeIndex = dictionary.Container.AddSplitNode(treeNode.Parent.NodeIndex, treeNode.ChildNum);
                    }
                    else
                    {
                        long blockOffset = treeNode.BlockOffset;
                        if (blockOffset < 0)
                        {
                            blockOffset = dictionary.Container.AllocateBlock();
                            newBlocks.Add(Tuple.Create(blockOffset, treeNode.Records));
                        }
                        else
                        {
                            dictionary.Container.WriteBlock(treeNode.BlockOffset, treeNode.Records);
                        }
                        treeNode.NodeIndex = dictionary.Container.AddDataNode(treeNode.Parent.NodeIndex, treeNode.ChildNum, blockOffset);
                    }
                }
            }

            foreach (Tuple<long, List<Record>> block in newBlocks)
            {
                dictionary.Container.WriteBlock(block.Item1, block.Item2);
            }
        }

        private SortedDictionary<long, BlockBatch> SplitByBlock(List<Record> records, bool addMissingBranches)
        {
            SortedDictionary<long, BlockBatch> blockBatches = new SortedDictionary<long, BlockBatch>();

            foreach (var record in records)
            {
                int maskOffset = 0;
                int nodeIndex = 0;
                CompactTreeNode node = dictionary.Container.GetTreeNode(nodeIndex);
                int childNum = 0;
                while (node.IsSplitNode)
                {
                    bool left = (record.Key[maskOffset/8] & (1 << (7 - maskOffset%8))) == 0;
                    childNum = left ? 0 : 1;
                    int childIndex = node.GetChild(childNum);
                    if (childIndex == 0)
                    {
                        break;
                    }
                    nodeIndex = childIndex;
                    node = dictionary.Container.GetTreeNode(nodeIndex);
                    maskOffset++;
                }

                if (node.IsSplitNode)
                {
                    if (addMissingBranches)
                    {
                        //todo: test this branch
                        long blockOffset = dictionary.Container.AllocateBlock();
                        nodeIndex = dictionary.Container.AddDataNode(nodeIndex, childNum, blockOffset);
                        node = dictionary.Container.GetTreeNode(nodeIndex);
                        maskOffset++;
                    }
                }

                if (node.IsDataNode)
                {
                    long blockOffset = node.Value;

                    BlockBatch blockBatch;
                    if (!blockBatches.TryGetValue(blockOffset, out blockBatch))
                    {
                        blockBatch = new BlockBatch(blockOffset, maskOffset, nodeIndex);
                        blockBatches.Add(blockOffset, blockBatch);
                    }

                    blockBatch.Records.Add(record);
                }
            }

            foreach (BlockBatch batch in blockBatches.Values)
            {
                batch.Records.Sort((recordA, recordB) => CompareKeys(recordA.Key, recordB.Key, batch.MaskOffset/8));
            }

            return blockBatches;
        }

        private List<SplitTreeNode> UpdateBlock(BlockBatch blockBatch)
        {
            Block block = dictionary.Container.ReadBlock(blockBatch.BlockOffset);

            //todo: review types
            List<Record> records = MergeRecords(block.Records, blockBatch.Records, blockBatch.MaskOffset/8);

            //todo: is it worth to have this check?
            if (records.Count <= dictionary.Container.RecordsPerBlock)
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
                    if (treeNode.Records.Count > dictionary.Container.RecordsPerBlock)
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
}