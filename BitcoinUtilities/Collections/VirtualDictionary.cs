using System;
using System.Collections.Generic;
using BitcoinUtilities.Collections.VirtualDictionaryInternals;

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

        private readonly CompactTree blockTree;

        private readonly VirtualDictionaryContainer container;

        private VirtualDictionary(string filename, int keySize, int valueSize)
        {
            blockTree = new CompactTree(CompactTreeNode.CreateDataNode(0));

            container = new VirtualDictionaryContainer(filename, keySize, valueSize);
            long firstBlockOffset = container.AllocateBlock();
            container.WriteBlock(firstBlockOffset, new List<Record>());
        }

        public static VirtualDictionary Open(string filename, int keySize, int valueSize)
        {
            return new VirtualDictionary(filename, keySize, valueSize);
        }

        public void Dispose()
        {
            //todo: implement
            container.Dispose();
        }

        internal VirtualDictionaryContainer Container
        {
            get { return container; }
        }

        public VirtualDictionaryTransaction BeginTransaction()
        {
            return new VirtualDictionaryTransaction(this);
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
    }
}