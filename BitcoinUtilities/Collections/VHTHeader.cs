using System;
using System.Text;

namespace BitcoinUtilities.Collections
{
    internal class VHTHeader
    {
        private static readonly byte[] prefix = Encoding.ASCII.GetBytes("VHT#");

        public int BlockSize { get; set; }

        public int RootBlocksCount { get; set; }
        public uint RootMask { get; set; }
        public int RootMaskLength { get; set; }

        public int ChildrenPerBlock { get; set; }
        public uint ChildrenMask { get; set; }
        public int ChildrenMaskLength { get; set; }

        public int RecordsPerBlock { get; set; }

        //todo: rename to size
        public int KeyLength { get; set; }
        //todo: rename to size
        public int ValueLength { get; set; }

        public long AllocatedSpace { get; set; }
        public long OccupiedSpace { get; set; }
        public long AllocationUnit { get; set; }

        //use struct instead of Copy?
        public VHTHeader Copy()
        {
            VHTHeader header = new VHTHeader();

            header.BlockSize = BlockSize;

            header.RootBlocksCount = RootBlocksCount;
            header.RootMask = RootMask;
            header.RootMaskLength = RootMaskLength;

            header.ChildrenPerBlock = ChildrenPerBlock;
            header.ChildrenMask = ChildrenMask;
            header.ChildrenMaskLength = ChildrenMaskLength;

            header.RecordsPerBlock = RecordsPerBlock;

            header.KeyLength = KeyLength;
            header.ValueLength = ValueLength;

            header.AllocatedSpace = AllocatedSpace;
            header.OccupiedSpace = OccupiedSpace;
            header.AllocationUnit = AllocationUnit;

            return header;
        }

        //use struct marshalling instead of manual copy?
        public byte[] GetBytes()
        {
            byte[] raw = new byte[BlockSize];

            int ofs = 0;

            Array.Copy(prefix, 0, raw, ofs, prefix.Length);
            ofs += prefix.Length;

            Array.Copy(BitConverter.GetBytes(BlockSize), 0, raw, ofs, sizeof (int));
            ofs += sizeof (int);

            Array.Copy(BitConverter.GetBytes(RootBlocksCount), 0, raw, ofs, sizeof(int));
            ofs += sizeof(int);
            Array.Copy(BitConverter.GetBytes(RootMask), 0, raw, ofs, sizeof(uint));
            ofs += sizeof (uint);
            Array.Copy(BitConverter.GetBytes(RootMaskLength), 0, raw, ofs, sizeof(int));
            ofs += sizeof(int);

            Array.Copy(BitConverter.GetBytes(ChildrenPerBlock), 0, raw, ofs, sizeof(int));
            ofs += sizeof(int);
            Array.Copy(BitConverter.GetBytes(ChildrenMask), 0, raw, ofs, sizeof(uint));
            ofs += sizeof (uint);
            Array.Copy(BitConverter.GetBytes(ChildrenMaskLength), 0, raw, ofs, sizeof(int));
            ofs += sizeof(int);

            Array.Copy(BitConverter.GetBytes(RecordsPerBlock), 0, raw, ofs, sizeof(int));
            ofs += sizeof(int);

            Array.Copy(BitConverter.GetBytes(KeyLength), 0, raw, ofs, sizeof(int));
            ofs += sizeof (int);
            Array.Copy(BitConverter.GetBytes(ValueLength), 0, raw, ofs, sizeof (int));
            ofs += sizeof (int);

            Array.Copy(BitConverter.GetBytes(AllocatedSpace), 0, raw, ofs, sizeof (long));
            ofs += sizeof (long);
            Array.Copy(BitConverter.GetBytes(OccupiedSpace), 0, raw, ofs, sizeof (long));
            ofs += sizeof (long);
            Array.Copy(BitConverter.GetBytes(AllocationUnit), 0, raw, ofs, sizeof (long));
            ofs += sizeof (long);

            return raw;
        }
    }
}