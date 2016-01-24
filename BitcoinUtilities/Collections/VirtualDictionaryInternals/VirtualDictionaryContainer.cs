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

        private readonly FileStream stream;
        private readonly BinaryWriter writer;
        private readonly BinaryReader reader;

        private long occupiedSpace = 0;
        private long allocatedSpace = 0;

        public VirtualDictionaryContainer(string filename, int keySize, int valueSize)
        {
            this.keySize = keySize;
            this.valueSize = valueSize;

            stream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            writer = new BinaryWriter(stream, Encoding.UTF8, true);
            reader = new BinaryReader(stream, Encoding.UTF8, true);
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
            writer.Flush();
        }
    }
}