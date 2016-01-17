using System;
using System.Collections.Generic;
using System.Linq;

namespace BitcoinUtilities.Collections
{
    public class VHTTransaction : IDisposable
    {
        private readonly VHTWriteAheadLog wal;

        //todo: set locally defined comparer
        //todo: rename to updatedRecords
        private readonly Dictionary<byte[], VHTRecord> newRecords = new Dictionary<byte[], VHTRecord>(ByteArrayComparer.Instance);
        private readonly SortedDictionary<long, VHTUpdate> unprocessedUpdates = new SortedDictionary<long, VHTUpdate>();

        internal VHTTransaction(VHTWriteAheadLog wal)
        {
            this.wal = wal;
        }

        public void AddOrUpdate(byte[] key, byte[] value)
        {
            //todo: check parameters' length
            newRecords[key] = new VHTRecord {Hash = wal.GetHash(key), Key = key, Value = value};
        }

        //todo: add test
        //todo: rollback on failure
        public void Commit()
        {
            foreach (VHTRecord record in newRecords.Values)
            {
                VHTBlock rootBlock = wal.ReadRootBlock(record);
                long blockOffset = rootBlock.Offset;

                VHTUpdate update;
                if (!unprocessedUpdates.TryGetValue(blockOffset, out update))
                {
                    update = CreateUpdate(rootBlock);
                    unprocessedUpdates.Add(blockOffset, update);
                }
                update.Records.Add(record);
            }

            while (unprocessedUpdates.Count > 0)
            {
                //todo: use set instead of dictionary?
                long offset = unprocessedUpdates.Keys.First();
                VHTUpdate update = unprocessedUpdates[offset];
                unprocessedUpdates.Remove(offset);

                ProcessUpdate(update);
            }

            newRecords.Clear();

            wal.Flush();
        }

        private void ProcessUpdate(VHTUpdate update)
        {
            if (update.Block == null)
            {
                update.Block = wal.ReadBlock(update.Offset, update.MaskOffset);
            }

            //todo: !!! contents of blocks should be merged here (thus allowing update)

            if (update.Block.Records.Count + update.Records.Count <= wal.Header.RecordsPerBlock)
            {
                update.Block.Records.AddRange(update.Records);
                wal.MarkDirty(update.Block);
                return;
            }

            List<VHTRecord>[] recordsByChild = new List<VHTRecord>[wal.Header.ChildrenPerBlock];
            for (int childIndex = 0; childIndex < wal.Header.ChildrenPerBlock; childIndex++)
            {
                recordsByChild[childIndex] = new List<VHTRecord>();
            }

            foreach (VHTRecord record in update.Block.Records)
            {
                uint childIndex = wal.GetChildIndex(update.Block, record);
                recordsByChild[childIndex].Add(record);
            }

            foreach (VHTRecord record in update.Records)
            {
                uint childIndex = wal.GetChildIndex(update.Block, record);
                recordsByChild[childIndex].Add(record);
            }

            for (int childIndex = 0; childIndex < wal.Header.ChildrenPerBlock; childIndex++)
            {
                List<VHTRecord> childRecords = recordsByChild[childIndex];
                if (childRecords.Count != 0)
                {
                    //todo: check that file access order is linear
                    VHTUpdate childUpdate = CreateChildUpdate(update.Block, childIndex, childRecords);
                    unprocessedUpdates.Add(childUpdate.Offset, childUpdate);
                }
            }

            //todo: should we keep some values in splitted node (e.g. if some child group is small enough)?
            update.Block.Records.Clear();
            //todo: sholud we always write parent? (No. Should not save if the block was and remained empty. And there are no new children.)
            wal.MarkDirty(update.Block);
        }

        private VHTUpdate CreateChildUpdate(VHTBlock parent, int childIndex, List<VHTRecord> childRecords)
        {
            VHTUpdate update;

            if (parent.ChildrenOffsets[childIndex] == 0)
            {
                int childMaskOffset = wal.GetChildMaskOffset(parent);
                VHTBlock childBlock = wal.CreateBlock(childMaskOffset);
                update = CreateUpdate(childBlock);

                parent.ChildrenOffsets[childIndex] = update.Offset;
                wal.MarkDirty(parent);
            }
            else
            {
                update = new VHTUpdate();
                update.Offset = parent.ChildrenOffsets[childIndex];
                update.MaskOffset = wal.GetChildMaskOffset(parent);
                update.Block = null;
            }

            update.Records = childRecords;

            return update;
        }

        public void Dispose()
        {
            wal.Dispose();
        }

        private VHTUpdate CreateUpdate(VHTBlock block)
        {
            VHTUpdate res = new VHTUpdate();
            res.Offset = block.Offset;
            res.MaskOffset = block.MaskOffset;
            res.Block = block;
            res.Records = new List<VHTRecord>();
            return res;
        }
    }

    //todo: move to separate file or remove
    internal class VHTUpdate
    {
        //todo: Offset and MaskOffset are redundant sometimes
        public long Offset { get; set; }
        public int MaskOffset { get; set; }
        public VHTBlock Block { get; set; }
        public List<VHTRecord> Records { get; set; }
    }
}