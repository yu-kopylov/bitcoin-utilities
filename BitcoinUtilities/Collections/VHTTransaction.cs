using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace BitcoinUtilities.Collections
{
    public class VHTTransaction : IDisposable
    {
        private readonly VHTWriteAheadLog wal;

        //todo: use locally defined comparer
        //todo: rename to updatedRecords
        private readonly Dictionary<byte[], VHTRecord> newRecords = new Dictionary<byte[], VHTRecord>(ByteArrayComparer.Instance);
        private readonly SortedDictionary<long, VHTProcessingBatch> unprocessedBatches = new SortedDictionary<long, VHTProcessingBatch>();

        internal VHTTransaction(VHTWriteAheadLog wal)
        {
            this.wal = wal;
        }

        public void AddOrUpdate(byte[] key, byte[] value)
        {
            //todo: check parameters' length
            newRecords[key] = new VHTRecord {Hash = wal.GetHash(key), Key = key, Value = value};
        }

        public Dictionary<byte[], byte[]> Find(List<byte[]> keys)
        {
            //todo: use locally defined comparer
            Dictionary<byte[], byte[]> res = new Dictionary<byte[], byte[]>(ByteArrayComparer.Instance);

            List<VHTRecord> recordsToLookup = new List<VHTRecord>();
            foreach (byte[] key in keys)
            {
                VHTRecord record;
                if (newRecords.TryGetValue(key, out record))
                {
                    res.Add(key, record.Value);
                }
                else
                {
                    recordsToLookup.Add(new VHTRecord {Hash = wal.GetHash(key), Key = key, Value = null});
                }
            }

            //todo: make Find thread-safe ?
            FindRecords(res, recordsToLookup);

            return res;
        }

        //todo: add test
        //todo: rollback on failure
        public void Commit()
        {
            UpdateRecords(newRecords.Values.ToList());
            AddRecords(newRecords.Values.ToList());

            newRecords.Clear();

            wal.Flush();
        }

        //todo: List or IEnumerable
        private void CreateBatches(List<VHTRecord> records)
        {
            foreach (VHTRecord record in records)
            {
                //todo: check arithmetic overflows
                //todo: remove wal.ReadRootBlock
                //VHTBlock rootBlock = wal.ReadRootBlock(record);
                uint rootIndex = record.Hash & wal.Header.RootMask;
                long blockOffset = (rootIndex + 1) * wal.Header.BlockSize;

                VHTProcessingBatch batch;
                if (!unprocessedBatches.TryGetValue(blockOffset, out batch))
                {
                    //batch = CreateBatch(rootBlock, new List<VHTRecord>());
                    //todo: use common method
                    batch = new VHTProcessingBatch();
                    batch.Offset = blockOffset;
                    batch.MaskOffset = 0;
                    batch.Block = null;
                    batch.Records = new List<VHTRecord>();
                    unprocessedBatches.Add(blockOffset, batch);
                }
                batch.Records.Add(record);
            }

            foreach (VHTProcessingBatch batch in unprocessedBatches.Values)
            {
                batch.Records.Sort(VHTRecordComparer.Instance);
            }
        }

        private void AddRecords(List<VHTRecord> records)
        {
            CreateBatches(records);

            while (unprocessedBatches.Count > 0)
            {
                //todo: use set instead of dictionary?
                long offset = unprocessedBatches.Keys.First();
                VHTProcessingBatch batch = unprocessedBatches[offset];
                unprocessedBatches.Remove(offset);

                AddRecords(batch);
            }
        }

        private void AddRecords(VHTProcessingBatch batch)
        {
            if (batch.Block == null)
            {
                batch.Block = wal.ReadBlock(batch.Offset, batch.MaskOffset);
            }

            List<VHTRecord> combinedRecords = Merge(
                batch.Records,
                batch.Block.Records,
                (a, b) =>
                {
                    Contract.Assert(a == null || b == null, "Duplicate keys should not be posible.");
                    return a ?? b;
                }
            );

            if (combinedRecords.Count <= wal.Header.RecordsPerBlock)
            {
                batch.Block.Records = combinedRecords;
                wal.MarkDirty(batch.Block);
                return;
            }

            List<VHTRecord>[] recordsByChild = SplitByChild(batch.Block, combinedRecords);

            batch.Block.Records.Clear();

            List<int> childIndexes = Enumerable.Range(0, wal.Header.ChildrenPerBlock).OrderBy(i => batch.Block.ChildrenOffsets[i] == 0).ThenBy(i => recordsByChild[i].Count).ToList();

            foreach (int childIndex in childIndexes)
            {
                List<VHTRecord> childRecords = recordsByChild[childIndex];
                if (childRecords.Count != 0)
                {
                    if (batch.Block.Records.Count + childRecords.Count <= wal.Header.RecordsPerBlock)
                    {
                        batch.Block.Records = Merge(batch.Block.Records, childRecords, (a, b) => a ?? b);
                    }
                    else if (batch.Block.Records.Count < wal.Header.RecordsPerBlock)
                    {
                        List<VHTRecord> recordsToKeep = childRecords.Take(wal.Header.RecordsPerBlock - batch.Block.Records.Count).ToList();
                        batch.Block.Records = Merge(batch.Block.Records, recordsToKeep, (a, b) => a ?? b);
                        childRecords = childRecords.Skip(wal.Header.RecordsPerBlock - batch.Block.Records.Count).ToList();
                        //todo: check that file access order is linear
                        VHTProcessingBatch childBatch = CreateChildBatch(batch.Block, childIndex, childRecords);
                        unprocessedBatches.Add(childBatch.Offset, childBatch);
                    }
                    else
                    {
                        //todo: check that file access order is linear
                        VHTProcessingBatch childBatch = CreateChildBatch(batch.Block, childIndex, childRecords);
                        unprocessedBatches.Add(childBatch.Offset, childBatch);
                    }
                }
            }

            //todo: sholud we always write parent? (No. Should not save if the block was and remained empty. And there are no new children.)
            wal.MarkDirty(batch.Block);
        }

        private void UpdateRecords(List<VHTRecord> records)
        {
            CreateBatches(records);

            while (unprocessedBatches.Count > 0)
            {
                //todo: use set instead of dictionary?
                long offset = unprocessedBatches.Keys.First();
                VHTProcessingBatch batch = unprocessedBatches[offset];
                unprocessedBatches.Remove(offset);

                UpdateRecords(batch);
            }
        }

        private void UpdateRecords(VHTProcessingBatch batch)
        {
            if (batch.Block == null)
            {
                batch.Block = wal.ReadBlock(batch.Offset, batch.MaskOffset);
            }

            bool blockUpdated = false;
            List<VHTRecord> missingRecords = Merge(
                batch.Block.Records,
                batch.Records,
                (a, b) =>
                {
                    if (a != null && b != null)
                    {
                        a.Value = b.Value;
                        //todo: compare values to avoid unnecessary updates?
                        blockUpdated = true;
                        //todo: keep cache and use another collection?
                        newRecords.Remove(b.Key);
                        return null;
                    }
                    return b;
                }
            );

            if (blockUpdated)
            {
                wal.MarkDirty(batch.Block);
            }

            //todo: split does some extra work (creates lists for non-existing children)
            List<VHTRecord>[] recordsByChild = SplitByChild(batch.Block, missingRecords);

            for (int childIndex = 0; childIndex < wal.Header.ChildrenPerBlock; childIndex++)
            {
                List<VHTRecord> childRecords = recordsByChild[childIndex];
                if (batch.Block.ChildrenOffsets[childIndex] != 0 && childRecords.Count != 0)
                {
                    VHTProcessingBatch childBatch = CreateChildBatch(batch.Block, childIndex, childRecords);
                    unprocessedBatches.Add(childBatch.Offset, childBatch);
                }
            }
        }

        private void FindRecords(Dictionary<byte[], byte[]> result, List<VHTRecord> records)
        {
            CreateBatches(records);

            while (unprocessedBatches.Count > 0)
            {
                //todo: use set instead of dictionary?
                long offset = unprocessedBatches.Keys.First();
                VHTProcessingBatch batch = unprocessedBatches[offset];
                unprocessedBatches.Remove(offset);

                FindRecords(result, batch);
            }
        }

        private void FindRecords(Dictionary<byte[], byte[]> result, VHTProcessingBatch batch)
        {
            if (batch.Block == null)
            {
                batch.Block = wal.ReadBlock(batch.Offset, batch.MaskOffset);
            }

            List<VHTRecord> missingRecords = Merge(
                batch.Block.Records,
                batch.Records,
                (a, b) =>
                {
                    if (a != null && b != null)
                    {
                        //todo: copy arrays to avoid possible corruption by client?
                        result.Add(b.Key, a.Value);
                        return null;
                    }
                    return b;
                }
            );

            //todo: split does some extra work (creates lists for non-existing children, cost probably is low)
            List<VHTRecord>[] recordsByChild = SplitByChild(batch.Block, missingRecords);

            for (int childIndex = 0; childIndex < wal.Header.ChildrenPerBlock; childIndex++)
            {
                List<VHTRecord> childRecords = recordsByChild[childIndex];
                if (batch.Block.ChildrenOffsets[childIndex] != 0 && childRecords.Count != 0)
                {
                    VHTProcessingBatch childBatch = CreateChildBatch(batch.Block, childIndex, childRecords);
                    unprocessedBatches.Add(childBatch.Offset, childBatch);
                }
            }
        }

        public void Dispose()
        {
            wal.Dispose();
        }

        //todo: add a delegate with XMLDOC for mergePair
        //todo: move to CollectionUtils
        private List<VHTRecord> Merge(List<VHTRecord> a, List<VHTRecord> b, Func<VHTRecord, VHTRecord, VHTRecord> mergePair)
        {
            VHTRecordComparer comparer = VHTRecordComparer.Instance;

            List<VHTRecord> res = new List<VHTRecord>(a.Count + b.Count);

            int pA = 0, pB = 0;
            while (pA < a.Count && pB < b.Count)
            {
                VHTRecord recA = a[pA];
                VHTRecord recB = b[pB];
                int diff = comparer.Compare(recA, recB);
                if (diff < 0)
                {
                    VHTRecord rec = mergePair(recA, null);
                    if (rec != null)
                    {
                        res.Add(rec);
                    }
                    pA++;
                }
                else if (diff > 0)
                {
                    VHTRecord rec = mergePair(null, recB);
                    if (rec != null)
                    {
                        res.Add(rec);
                    }
                    pB++;
                }
                else
                {
                    VHTRecord rec = mergePair(recA, recB);
                    if (rec != null)
                    {
                        res.Add(rec);
                    }
                    pA++;
                    pB++;
                }
            }

            for (; pA < a.Count; pA++)
            {
                VHTRecord rec = mergePair(a[pA], null);
                if (rec != null)
                {
                    res.Add(rec);
                }
            }

            for (; pB < b.Count; pB++)
            {
                VHTRecord rec = mergePair(null, b[pB]);
                if (rec != null)
                {
                    res.Add(rec);
                }
            }

            return res;
        }

        private List<VHTRecord>[] SplitByChild(VHTBlock block, List<VHTRecord> records)
        {
            List<VHTRecord>[] recordsByChild = new List<VHTRecord>[wal.Header.ChildrenPerBlock];
            for (int childIndex = 0; childIndex < wal.Header.ChildrenPerBlock; childIndex++)
            {
                recordsByChild[childIndex] = new List<VHTRecord>();
            }
            foreach (VHTRecord record in records)
            {
                uint childIndex = wal.GetChildIndex(block, record);
                recordsByChild[childIndex].Add(record);
            }
            return recordsByChild;
        }

        private VHTProcessingBatch CreateChildBatch(VHTBlock parent, int childIndex, List<VHTRecord> childRecords)
        {
            VHTProcessingBatch batch;

            if (parent.ChildrenOffsets[childIndex] == 0)
            {
                int childMaskOffset = wal.GetChildMaskOffset(parent);
                VHTBlock childBlock = wal.CreateBlock(childMaskOffset);
                batch = CreateBatch(childBlock, childRecords);

                parent.ChildrenOffsets[childIndex] = batch.Offset;
                wal.MarkDirty(parent);
            }
            else
            {
                //todo: use common method
                batch = new VHTProcessingBatch();
                batch.Offset = parent.ChildrenOffsets[childIndex];
                batch.MaskOffset = wal.GetChildMaskOffset(parent);
                batch.Block = null;
                batch.Records = childRecords;
            }

            return batch;
        }

        private VHTProcessingBatch CreateBatch(VHTBlock block, List<VHTRecord> records)
        {
            VHTProcessingBatch res = new VHTProcessingBatch();
            res.Offset = block.Offset;
            res.MaskOffset = block.MaskOffset;
            res.Block = block;
            res.Records = records;
            return res;
        }
    }

    //todo: move to separate file or remove
    internal class VHTProcessingBatch
    {
        //todo: Offset and MaskOffset are redundant sometimes
        public long Offset { get; set; }
        public int MaskOffset { get; set; }
        public VHTBlock Block { get; set; }
        public List<VHTRecord> Records { get; set; }
    }

    internal class VHTKeyComparer : IComparer<byte[]>
    {
        private static readonly VHTKeyComparer instance = new VHTKeyComparer();

        private VHTKeyComparer()
        {
        }

        public static VHTKeyComparer Instance
        {
            get { return instance; }
        }

        public int Compare(byte[] x, byte[] y)
        {
            int commonKeyLength = x.Length;
            if (y.Length < commonKeyLength)
            {
                commonKeyLength = y.Length;
            }
            for (int i = 0; i < commonKeyLength; i++)
            {
                int r = x[i].CompareTo(y[i]);
                if (r != 0)
                {
                    return i;
                }
            }
            return x.Length.CompareTo(y.Length);
        }
    }

    internal class VHTRecordComparer : IComparer<VHTRecord>
    {
        private static readonly VHTRecordComparer instance = new VHTRecordComparer();
        private static readonly VHTKeyComparer keyComparer = VHTKeyComparer.Instance;

        private VHTRecordComparer()
        {
        }

        public static VHTRecordComparer Instance
        {
            get { return instance; }
        }

        public int Compare(VHTRecord x, VHTRecord y)
        {
            int r = x.Hash.CompareTo(y.Hash);
            if (r != 0)
            {
                return r;
            }

            return keyComparer.Compare(x.Key, y.Key);
        }
    }
}