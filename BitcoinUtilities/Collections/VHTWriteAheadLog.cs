using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BitcoinUtilities.Collections
{
    internal class VHTWriteAheadLog : IDisposable
    {
        private const int MarkerLength = 16;
        private static readonly byte[] incompleteMarker = Encoding.ASCII.GetBytes("WAL:INCOMPLETE__");
        private static readonly byte[] completeMarker = Encoding.ASCII.GetBytes("WAL:COMPLETE____");
        private static readonly byte[] emptyMarker = Encoding.ASCII.GetBytes("WAL:EMPTY_______");

        private const int HeaderLength = MarkerLength + 2*8;

        private readonly VirtualHashTable table;

        private readonly VHTHeader header;
        private readonly Dictionary<long, VHTBlock> blocks = new Dictionary<long, VHTBlock>();
        private readonly SortedSet<long> affectedBlocks = new SortedSet<long>();

        private readonly string filename;
        private FileStream stream;

        private ulong checksum;

        public VHTWriteAheadLog(VirtualHashTable table, string filename)
        {
            this.table = table;
            this.filename = filename;
            //todo: is FileShare.Read acceptable ?
            stream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            header = table.Header.Copy();
        }

        public VHTHeader Header
        {
            get { return header;}
        }

        public void Dispose()
        {
            //todo: is it correct to just close log?
            if (stream != null)
            {
                stream.Close();
                stream = null;
            }
            //todo: delete file
        }

        public VHTBlock CreateBlock(int maskOffset)
        {
            VHTBlock block = new VHTBlock();
            block.Offset = header.OccupiedSpace;
            block.MaskOffset = maskOffset;

            block.ChildrenOffsets = new long[header.ChildrenPerBlock];
            block.Records = new List<VHTRecord>(header.RecordsPerBlock);

            blocks.Add(block.Offset, block);
            affectedBlocks.Add(block.Offset);

            header.OccupiedSpace += header.BlockSize;
            while (header.AllocatedSpace < header.OccupiedSpace)
            {
                //todo: allow zero AllocationUnit ?
                header.AllocatedSpace = (header.OccupiedSpace + header.AllocationUnit - 1) / header.AllocationUnit * header.AllocationUnit;
            }
            return block;
        }

        public VHTBlock ReadBlock(long offset, int maskOffset)
        {
            VHTBlock block;
            if (blocks.TryGetValue(offset, out block))
            {
                return block;
            }

            block = table.ReadBlock(offset, maskOffset);
            blocks.Add(block.Offset, block);

            return block;
        }

        public VHTBlock ReadRootBlock(VHTRecord record)
        {
            uint rootIndex = record.Hash & header.RootMask;
            //todo: root blocks should be cached to avoid random access
            return ReadBlock(header.BlockSize * (rootIndex + 1), 0);
        }

        //todo: if VHTBlock is struct, use offset as parameter and check that block exists
        public void MarkDirty(VHTBlock block)
        {
            affectedBlocks.Add(block.Offset);
        }

        //todo: can this warapper be useful?
        public uint GetHash(byte[] value)
        {
            return table.GetHash(value);
        }

        public int GetChildMaskOffset(VHTBlock parent)
        {
            if (parent.MaskOffset == 0)
            {
                return header.RootMaskLength;
            }
            return parent.MaskOffset + header.ChildrenMaskLength;
        }

        //todo: review and write good tests
        //todo: allow arbitrary number of children using % operator?
        //todo: move to more specialized class?
        public uint GetChildIndex(VHTBlock block, VHTRecord record)
        {
            uint index = 0;

            if (block.MaskOffset < 32)
            {
                index = (record.Hash >> block.MaskOffset) & header.ChildrenMask;
            }

            if (block.MaskOffset == 0)
            {
                return index & header.ChildrenMask;
            }

            int keyMaskOffset = block.MaskOffset - 32;
            uint keyMask = header.ChildrenMask;

            int keyOffset = 0;
            int keyMaskShift = 0;

            while (keyMask != 0 && keyOffset < header.KeyLength)
            {
                if (keyMaskOffset < 0)
                {
                    int shift = -keyMaskOffset;

                    keyMask = header.ChildrenMask >> shift;
                    keyMaskShift -= shift;
                    keyMaskOffset = 0;
                }

                if (keyMaskOffset < 8 && keyMaskShift >= 0)
                {
                    uint indexPart = (uint)record.Key[keyOffset] >> keyMaskOffset;
                    indexPart = indexPart & keyMask;
                    index |= indexPart << keyMaskShift;
                }

                keyOffset++;
                keyMaskOffset -= 8;
            }

            return index;
        }

        public void Flush()
        {
            //todo: is sync required ?

            PrepareSave();
            SaveContent();
            ApplyContent();
            ClearContent();
        }

        private void PrepareSave()
        {
            stream.SetLength(HeaderLength + header.BlockSize + (8 + header.BlockSize) * affectedBlocks.Count);
            stream.Position = 0;

            checksum = 23;
            WriteHeader(incompleteMarker);

            stream.Flush();
        }

        private void SaveContent()
        {
            byte[] headerRaw = header.GetBytes();
            WriteBytes(headerRaw, true);

            foreach (long offset in affectedBlocks)
            {
                VHTBlock block = blocks[offset];
                WriteLong(block.Offset, true);
                byte[] blockContent = block.GetBytes(header);
                WriteBytes(blockContent, true);
            }

            stream.Position = 0;
            WriteHeader(completeMarker);
            stream.Flush();
        }

        private void ApplyContent()
        {
            table.ApplyChanges(header, affectedBlocks.Select(ofs => blocks[ofs]).ToList());
        }

        private void ClearContent()
        {
            affectedBlocks.Clear();
            blocks.Clear();

            stream.Position = 0;
            WriteHeader(emptyMarker);
            stream.Flush();
        }

        private void WriteHeader(byte[] marker)
        {
            WriteBytes(marker, false);
            WriteLong(affectedBlocks.Count, false);
            WriteLong((long) checksum, false);
        }

        private void WriteLong(long value, bool updateChecksum)
        {
            byte[] raw = BitConverter.GetBytes(value);
            WriteBytes(raw, updateChecksum);
        }

        private void WriteBytes(byte[] value, bool updateChecksum)
        {
            stream.Write(value, 0, value.Length);

            if (updateChecksum)
            {
                foreach (byte b in value)
                {
                    //todo: use better checksum
                    checksum = checksum*31 + b;
                }
            }
        }
    }
}