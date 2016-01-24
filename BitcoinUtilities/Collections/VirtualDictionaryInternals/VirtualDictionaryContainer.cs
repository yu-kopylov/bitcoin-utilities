using System;
using System.Collections.Generic;
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

        private readonly FileStream stream;
        private readonly BinaryWriter writer;
        private readonly BinaryReader reader;

        private long occupiedSpace;
        private long allocatedSpace;

        private readonly List<long> treeBlockOffsets = new List<long>();
        private CompactTree readonlyBlockTree;
        private CompactTree updatableBlockTree;

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
            readonlyBlockTree = new CompactTree(CompactTreeNode.CreateDataNode(firstDataBlockOffset));
        }

        public void Dispose()
        {
            writer.Close();
            reader.Close();
            stream.Close();
        }

        public CompactTree ReadonlyBlockTree
        {
            get { return readonlyBlockTree; }
        }

        public CompactTree UpdatableBlockTree
        {
            get
            {
                if (updatableBlockTree == null)
                {
                    updatableBlockTree = new CompactTree();
                    updatableBlockTree.Nodes.AddRange(readonlyBlockTree.Nodes);
                }
                return updatableBlockTree;
            }
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

        public void Commit()
        {
            if (updatableBlockTree != null)
            {
                WriteTree();
                readonlyBlockTree = updatableBlockTree;
                updatableBlockTree = null;
            }
            writer.Flush();
        }

        private void WriteTree()
        {
            //todo: node removal is not supported
            int blocksRequired = (updatableBlockTree.Nodes.Count + treeNodesPerBlock + 1)/treeNodesPerBlock;

            while (treeBlockOffsets.Count < blocksRequired)
            {
                long blockOffset = AllocateBlock();
                treeBlockOffsets.Add(blockOffset);
            }

            int blockNum = 0;
            for (; blockNum < treeBlockOffsets.Count; blockNum++)
            {
                int blockRangeStart = blockNum*treeNodesPerBlock;
                bool blockUpdated = false;
                for (int i = 0; i < treeNodesPerBlock; i++)
                {
                    int nodeIndex = blockRangeStart + i;
                    if (nodeIndex >= readonlyBlockTree.Nodes.Count)
                    {
                        blockUpdated = nodeIndex < updatableBlockTree.Nodes.Count;
                        break;
                    }
                    if (readonlyBlockTree.Nodes[nodeIndex].RawData != updatableBlockTree.Nodes[nodeIndex].RawData)
                    {
                        blockUpdated = true;
                        break;
                    }
                }
                if (blockUpdated)
                {
                    WriteTreeBlock(blockNum);
                }
            }
        }

        private void WriteTreeBlock(int blockNum)
        {
            long nextBlockOffset = (blockNum + 1) < treeBlockOffsets.Count ? treeBlockOffsets[blockNum + 1] : -1;
            int rangeStart = blockNum*treeNodesPerBlock;
            int recordsCount = updatableBlockTree.Nodes.Count - rangeStart;
            if (recordsCount > treeNodesPerBlock)
            {
                recordsCount = treeNodesPerBlock;
            }
            stream.Position = treeBlockOffsets[blockNum];
            writer.Write(nextBlockOffset);
            writer.Write(recordsCount);
            for (int i = 0; i < recordsCount; i++)
            {
                writer.Write(updatableBlockTree.Nodes[rangeStart + i].RawData);
            }
        }
    }
}