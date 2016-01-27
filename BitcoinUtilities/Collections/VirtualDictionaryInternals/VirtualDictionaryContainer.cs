using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;

namespace BitcoinUtilities.Collections.VirtualDictionaryInternals
{
    internal class VirtualDictionaryContainer : IDisposable
    {
        private readonly int keySize;
        private readonly int valueSize;
        private readonly int blockSize = 4096;
        private readonly int allocationUnit = 1024*1024;
        private readonly int treeNodesPerBlock = 510;
        private readonly int recordsPerBlock = 128;

        private readonly FileStream stream;
        private readonly BinaryWriter writer;
        private readonly BinaryReader reader;

        private long occupiedSpace;
        private long allocatedSpace;

        private readonly List<long> treeBlockOffsets = new List<long>();
        private readonly List<bool> dirtyTreeBlocks = new List<bool>();
        private readonly CompactTree tree;

        public VirtualDictionaryContainer(string filename, int keySize, int valueSize)
        {
            this.keySize = keySize;
            this.valueSize = valueSize;

            stream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            writer = new BinaryWriter(stream, Encoding.UTF8, true);
            reader = new BinaryReader(stream, Encoding.UTF8, true);

            long firstTreeBlockOffset = AllocateBlock();
            long firstDataBlockOffset = AllocateBlock();

            treeBlockOffsets.Add(firstTreeBlockOffset);
            dirtyTreeBlocks.Add(true);

            tree = new CompactTree(CompactTreeNode.CreateDataNode(firstDataBlockOffset));
        }

        public void Dispose()
        {
            writer.Close();
            reader.Close();
            stream.Close();
        }

        public Block ReadBlock(long offset)
        {
            Block block = new Block();

            stream.Position = offset;
            int count = reader.ReadInt16();
            for (int i = 0; i < count; i++)
            {
                byte[] key = reader.ReadBytes(keySize);
                byte[] value = reader.ReadBytes(valueSize);
                Record record = new Record(key, value);
                block.Records.Add(record);
            }

            return block;
        }

        public void WriteBlock(long offset, List<Record> records)
        {
            Contract.Assert(records.Count <= recordsPerBlock);
            
            stream.Position = offset;
            writer.Write((short) records.Count);
            foreach (var record in records)
            {
                writer.Write(record.Key, 0, record.Key.Length);
                writer.Write(record.Value, 0, record.Value.Length);
            }
        }

        public long AllocateBlock()
        {
            long offset = occupiedSpace;
            occupiedSpace += blockSize;
            if (occupiedSpace > allocatedSpace)
            {
                allocatedSpace = (occupiedSpace + allocationUnit - 1)/allocationUnit*allocationUnit;
                stream.SetLength(allocatedSpace);
            }
            return offset;
        }

        public CompactTreeNode GetTreeNode(int nodeIndex)
        {
            return tree.Nodes[nodeIndex];
        }

        public void SplitDataNode(int nodeIndex)
        {
            Contract.Assert(tree.Nodes[nodeIndex].IsDataNode);
            tree.Nodes[nodeIndex] = CompactTreeNode.CreateSplitNode();
            MarkTreeNodeDirty(nodeIndex);
        }

        public int AddSplitNode(int nodeIndex, int childNum)
        {
            int childIndex = tree.AddSplitNode(nodeIndex, childNum);
            MarkTreeNodeDirty(nodeIndex);
            MarkTreeNodeDirty(childIndex);
            return childIndex;
        }

        public int AddDataNode(int nodeIndex, int childNum, long value)
        {
            int childIndex = tree.AddDataNode(nodeIndex, childNum, value);
            MarkTreeNodeDirty(nodeIndex);
            MarkTreeNodeDirty(childIndex);
            return childIndex;
        }

        private void MarkTreeNodeDirty(int nodeIndex)
        {
            int blockNum = nodeIndex / treeNodesPerBlock;
            if (blockNum < dirtyTreeBlocks.Count)
            {
                dirtyTreeBlocks[blockNum] = true;
            }
            else
            {
                dirtyTreeBlocks.Add(true);
                treeBlockOffsets.Add(AllocateBlock());
            }
        }
        
        public void Commit()
        {
            WriteTree();
            writer.Flush();
        }

        private void WriteTree()
        {
            for (int i = 0; i < treeBlockOffsets.Count; i++)
            {
                if (dirtyTreeBlocks[i])
                {
                    WriteTreeBlock(i);
                }
            }
        }

        private void WriteTreeBlock(int blockNum)
        {
            long nextBlockOffset = (blockNum + 1) < treeBlockOffsets.Count ? treeBlockOffsets[blockNum + 1] : -1;
            int rangeStart = blockNum*treeNodesPerBlock;
            int recordsCount = tree.Nodes.Count - rangeStart;
            if (recordsCount > treeNodesPerBlock)
            {
                recordsCount = treeNodesPerBlock;
            }
            stream.Position = treeBlockOffsets[blockNum];
            writer.Write(nextBlockOffset);
            writer.Write(recordsCount);
            for (int i = 0; i < recordsCount; i++)
            {
                writer.Write(tree.Nodes[rangeStart + i].RawData);
            }
        }
    }
}