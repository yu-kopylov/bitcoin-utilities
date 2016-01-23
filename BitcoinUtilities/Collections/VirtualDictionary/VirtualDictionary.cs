using System;
using System.Collections.Generic;

namespace BitcoinUtilities.Collections
{
    public partial class VirtualDictionary : IDisposable
    {
        //todo: use to accumulate small changes
        private readonly List<NonIndexedRecord> nonIndexedRecords = new List<NonIndexedRecord>();
        //todo: use local comparer
        private readonly Dictionary<byte[], int> nonIndexedRecordsByKey = new Dictionary<byte[], int>(ByteArrayComparer.Instance);

        private VirtualDictionary(string filename, int keySize, int valueSize)
        {
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
        private class NonIndexedRecord
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

            List<byte[]> keysToLookup = new List<byte[]>();

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
                    keysToLookup.Add(key);
                }
            }

            //todo: implement search for saved keys

            return res;
        }

        public void Commit()
        {
            //todo: implement
            foreach (KeyValuePair<byte[], byte[]> unsavedRecord in unsavedRecords)
            {
                dictionary.AddNonIndexedValue(unsavedRecord.Key, unsavedRecord.Value);
            }
        }

        public void Rollback()
        {
            //todo: inplement
        }
    }
}
