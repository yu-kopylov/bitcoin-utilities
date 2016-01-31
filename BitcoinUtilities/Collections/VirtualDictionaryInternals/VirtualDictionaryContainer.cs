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
        private readonly int blockSize = 4*1024;
        private readonly int allocationUnit = 1024*1024;
        private readonly int treeNodesPerBlock;
        private readonly int recordsPerBlock;
        private readonly int nonIndexedRecordsPerBlock;
        private readonly int maxNonIndexedRecordsCount = 1000;

        private readonly AtomicStream stream;
        private readonly BinaryWriter writer;
        private readonly BinaryReader reader;

        private long occupiedSpace;
        private long allocatedSpace;

        private readonly List<long> treeBlockOffsets = new List<long>();
        private readonly List<bool> dirtyTreeBlocks = new List<bool>();
        private readonly CompactTree tree;

        private readonly List<long> nonIndexedBlockOffsets = new List<long>();
        private readonly List<bool> dirtyNonIndexedBlocks = new List<bool>();
        //todo: set initial capacity
        private List<Record> nonIndexedRecords = new List<Record>();
        //todo: use local comparer
        private Dictionary<byte[], int> nonIndexedRecordsByKey = new Dictionary<byte[], int>(ByteArrayComparer.Instance);

        public VirtualDictionaryContainer(string filename, int keySize, int valueSize)
        {
            this.keySize = keySize;
            this.valueSize = valueSize;

            recordsPerBlock = (blockSize - 2)/(keySize + valueSize);
            nonIndexedRecordsPerBlock = (blockSize - 12)/8;
            treeNodesPerBlock = (blockSize - 12) / 8;

            FileStream mainStream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            FileStream walStream = new FileStream(filename + "-wal", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

            if (mainStream.Length != 0)
            {
                throw new Exception("Reopening of an existing database is not supported yet.");
            }

            stream = new AtomicStream(mainStream, walStream);
            writer = new BinaryWriter(stream, Encoding.UTF8, true);
            reader = new BinaryReader(stream, Encoding.UTF8, true);

            long firstTreeBlockOffset = AllocateBlock();
            treeBlockOffsets.Add(firstTreeBlockOffset);
            dirtyTreeBlocks.Add(true);

            int nonIndexedBlockCount = (maxNonIndexedRecordsCount + nonIndexedRecordsPerBlock - 1)/nonIndexedRecordsPerBlock;
            for (int i = 0; i < nonIndexedBlockCount; i++)
            {
                nonIndexedBlockOffsets.Add(AllocateBlock());
                dirtyNonIndexedBlocks.Add(true);
            }

            long firstDataBlockOffset = AllocateBlock();
            tree = new CompactTree(CompactTreeNode.CreateDataNode(firstDataBlockOffset));

            Commit();
        }

        public void Dispose()
        {
            writer.Close();
            reader.Close();
            stream.Close();
        }

        public int RecordsPerBlock
        {
            get { return recordsPerBlock; }
        }

        public bool FlushToDisk 
        {
            get { return stream.FlushToDisk; }
            set { stream.FlushToDisk = value; }
        }

        public Block ReadBlock(long offset)
        {
            stream.Position = offset;
            int count = reader.ReadInt16();

            byte[] blockData = reader.ReadBytes(count * (keySize + valueSize));
            Record[] records = new Record[count];

            for (int i = 0, recordOffset = 0; i < count; i++, recordOffset += keySize + valueSize)
            {
                ByteArrayRef key = new ByteArrayRef(blockData, recordOffset, keySize);
                ByteArrayRef value = new ByteArrayRef(blockData, recordOffset + keySize, valueSize);

                Record record = new Record(key, value);
                records[i] = record;
            }

            return new Block(records);
        }

        public void WriteBlock(long offset, List<Record> records)
        {
            Contract.Assert(records.Count <= recordsPerBlock);

            stream.Position = offset;
            writer.Write((short) records.Count);
            foreach (var record in records)
            {
                writer.Write(record.Key.Array, record.Key.Position, record.Key.Length);
                writer.Write(record.Value.Array, record.Value.Position, record.Value.Length);
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

        //todo: align names (nonIndexedValue vs nonIndexedRecords)
        public bool TryGetNonIndexedValue(byte[] key, out byte[] value)
        {
            int recordIndex;
            if (nonIndexedRecordsByKey.TryGetValue(key, out recordIndex))
            {
                value = nonIndexedRecords[recordIndex].Value.ToArray();
                return true;
            }
            value = null;
            return false;
        }

        //todo: add XMLDOC
        public bool TryAddNonIndexedValue(byte[] key, byte[] value)
        {
            //todo: implement correctly (depends on record being class or struct)
            Record record = new Record(key, value);
            int recordIndex;
            if (nonIndexedRecordsByKey.TryGetValue(key, out recordIndex))
            {
                nonIndexedRecords[recordIndex] = record;
                MarkNonIndexedValueDirty(recordIndex);
                return true;
            }

            recordIndex = nonIndexedRecords.Count;

            if (recordIndex < maxNonIndexedRecordsCount)
            {
                nonIndexedRecords.Add(record);
                nonIndexedRecordsByKey.Add(key, recordIndex);
                MarkNonIndexedValueDirty(recordIndex);
                return true;
            }

            return false;
        }

        public List<Record> ClearNonIndexedValues()
        {
            for (int i = 0, rangeStart = 0; i < dirtyNonIndexedBlocks.Count; i++, rangeStart += nonIndexedRecordsPerBlock)
            {
                if (rangeStart >= nonIndexedRecords.Count)
                {
                    break;
                }
                dirtyNonIndexedBlocks[i] = true;
            }

            List<Record> res = nonIndexedRecords;
            nonIndexedRecords = new List<Record>(maxNonIndexedRecordsCount);
            //todo: use local comparer
            nonIndexedRecordsByKey = new Dictionary<byte[], int>(ByteArrayComparer.Instance);
            return res;
        }

        private void MarkNonIndexedValueDirty(int recordIndex)
        {
            int blockNum = recordIndex/nonIndexedRecordsPerBlock;
            dirtyNonIndexedBlocks[blockNum] = true;
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
            int blockNum = nodeIndex/treeNodesPerBlock;
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
            //todo: make write linear?
            WriteNonIndexedRecords();
            WriteTree();

            //todo: is this flush useful? 
            writer.Flush();

            stream.Commit();
        }

        private void WriteNonIndexedRecords()
        {
            for (int i = 0; i < nonIndexedBlockOffsets.Count; i++)
            {
                if (dirtyNonIndexedBlocks[i])
                {
                    WriteNonIndexedBlock(i);
                }
            }
        }

        private void WriteNonIndexedBlock(int blockNum)
        {
            long nextBlockOffset = (blockNum + 1) < nonIndexedBlockOffsets.Count ? nonIndexedBlockOffsets[blockNum + 1] : -1;
            int rangeStart = blockNum*nonIndexedRecordsPerBlock;
            int recordsCount = nonIndexedRecords.Count - rangeStart;
            if (recordsCount < 0)
            {
                recordsCount = 0;
            }
            else if (recordsCount > nonIndexedRecordsPerBlock)
            {
                recordsCount = nonIndexedRecordsPerBlock;
            }
            stream.Position = nonIndexedBlockOffsets[blockNum];
            writer.Write(nextBlockOffset);
            writer.Write(recordsCount);
            for (int i = 0; i < recordsCount; i++)
            {
                writer.Write(tree.Nodes[rangeStart + i].RawData);
            }
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