using System;
using System.Collections.Generic;

namespace BitcoinUtilities.Collections
{
    internal class VHTBlock
    {
        public long[] ChildrenOffsets { get; set; }
        public List<VHTRecord> Records { get; set; }

        //calculated fields
        public int MaskOffset { get; set; }
        public long Offset { get; set; }

        public byte[] GetBytes(VHTHeader header)
        {
            byte[] raw = new byte[header.BlockSize];

            int inBlockOffset = 0;
            for (int childIndex = 0; childIndex < header.ChildrenPerBlock; childIndex++)
            {
                byte[] val = BitConverter.GetBytes(ChildrenOffsets[childIndex]);
                Array.Copy(val, 0, raw, inBlockOffset, 8);
                inBlockOffset += 8;
            }

            {
                byte[] val = BitConverter.GetBytes((ushort) Records.Count);
                Array.Copy(val, 0, raw, inBlockOffset, 2);
                inBlockOffset += 2;
            }

            foreach (VHTRecord record in Records)
            {
                Array.Copy(record.Key, 0, raw, inBlockOffset, header.KeyLength);
                inBlockOffset += header.KeyLength;

                Array.Copy(record.Value, 0, raw, inBlockOffset, header.ValueLength);
                inBlockOffset += header.ValueLength;
            }

            return raw;
        }
    }
}