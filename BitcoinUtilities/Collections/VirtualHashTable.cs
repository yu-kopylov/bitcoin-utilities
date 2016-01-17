using System;
using System.Collections.Generic;
using System.IO;

namespace BitcoinUtilities.Collections
{
    //todo: describe class
    //todo: describe thread-safety
    public class VirtualHashTable : IDisposable
    {
        /// <summary>
        /// The minimum block size that can accomodate a table header.
        /// </summary>
        private const int MinBlockSize = 68;//todo: calculate sizeof(VHTHeader); 

        private readonly string filename;

        private VHTHeader header;

        //todo: this cache is not used currently
        private VHTBlock[] rootNodes;

        //todo: test with memory-mapped file
        private FileStream stream;

        private VirtualHashTable(VHTSettings settings)
        {
            filename = settings.Filename;
            
            header = new VHTHeader();
            
            //todo: validate settings

            header.BlockSize = -1;

            header.RootBlocksCount = 16;
            header.RootMask = 0xF;
            header.RootMaskLength = 4;

            header.ChildrenPerBlock = 2;
            header.ChildrenMask = 0x1;
            header.ChildrenMaskLength = 1;

            header.RecordsPerBlock = 16;

            header.KeyLength = settings.KeyLength;
            header.ValueLength = settings.ValueLength;

            header.AllocatedSpace = 0;
            header.OccupiedSpace = 0;
            header.AllocationUnit = 4096;

            //todo: set or calculate BlockSize ?
            header.BlockSize = header.ChildrenPerBlock*8 + 2 + header.RecordsPerBlock * (header.KeyLength + header.ValueLength);
        }

        internal VHTHeader Header
        {
            get { return header; }
        }

        public static VirtualHashTable Open(VHTSettings settings)
        {
            VirtualHashTable table = new VirtualHashTable(settings);
            //todo: is this a correct pattern?
            try
            {
                table.Init();
            }
            catch (Exception)
            {
                table.Dispose();
                throw;
            }
            return table;
        }

        private void Init()
        {
            //todo: is FileShare.Read acceptable ?
            stream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

            if (stream.Length > 0 && stream.Length < MinBlockSize)
            {
                //todo: describe exceptions
                throw new Exception("File is not a valid VirtualHashTable file.");
            }

            bool isNew = stream.Length == 0;

            if (isNew)
            {
                //todo: persist header
                //todo: allocate space in big chunks
                header.OccupiedSpace = (1 + header.RootBlocksCount) * header.BlockSize;
                //todo: use allocation unit
                header.AllocatedSpace = header.OccupiedSpace;

                stream.SetLength(header.AllocatedSpace);
                stream.Write(header.GetBytes(), 0, header.BlockSize);

                rootNodes = new VHTBlock[header.RootBlocksCount];
                for (int i = 0; i < header.RootBlocksCount; i++)
                {
                    VHTBlock block = new VHTBlock();
                    rootNodes[i] = block;
                    block.ChildrenOffsets = new long[header.ChildrenPerBlock];
                    block.Records = new List<VHTRecord>();

                    block.Offset = header.BlockSize * (i + 1);
                    block.MaskOffset = 0;

                    WriteBlock(block);
                }
            }
            else
            {
                throw new NotImplementedException("Reopening is not implemented yet.");

                //todo: read header and compare it values with settings
                for (int i = 0; i < header.RootBlocksCount; i++)
                {
                    rootNodes[i] = ReadBlock(header.BlockSize * (i + 1), 0);
                }
            }
        }

        public void Dispose()
        {
            if (stream != null)
            {
                stream.Dispose();
            }
        }

        private void WriteBlock(VHTBlock block)
        {
            stream.Position = block.Offset;
            stream.Write(block.GetBytes(header), 0, header.BlockSize);
        }

        //todo: maskOffset looks ugly here
        internal VHTBlock ReadBlock(long offset, int maskOffset)
        {
            byte[] rawBlock = new byte[header.BlockSize];

            //todo: test with memory-mapped file
            //todo: whats the difference between seek and position 
            stream.Position = offset;
            //todo: does it always read specified number of bytes?
            int bytesRead = stream.Read(rawBlock, 0, header.BlockSize);
            if (bytesRead != header.BlockSize)
            {
                //todo: specity exception
                throw new Exception("Unexpected end of file.");
            }

            VHTBlock block = new VHTBlock();
            block.Offset = offset;
            block.MaskOffset = maskOffset;

            block.ChildrenOffsets = new long[header.ChildrenPerBlock];
            int inBlockOffset = 0;
            for (int childIndex = 0; childIndex < header.ChildrenPerBlock; childIndex++)
            {
                block.ChildrenOffsets[childIndex] = BitConverter.ToInt64(rawBlock, inBlockOffset);
                inBlockOffset += 8;
            }

            int recordsCount = BitConverter.ToUInt16(rawBlock, inBlockOffset);
            inBlockOffset += 2;

            block.Records = new List<VHTRecord>(header.RecordsPerBlock);

            for (int recordIndex = 0; recordIndex < recordsCount; recordIndex++)
            {
                VHTRecord record = new VHTRecord();
                record.Key = new byte[header.KeyLength];
                record.Value = new byte[header.ValueLength];

                Array.Copy(rawBlock, inBlockOffset, record.Key, 0, header.KeyLength);
                inBlockOffset += header.KeyLength;

                Array.Copy(rawBlock, inBlockOffset, record.Value, 0, header.ValueLength);
                inBlockOffset += header.ValueLength;

                record.Hash = GetHash(rawBlock);
            }

            return block;
        }

        internal void ApplyChanges(VHTHeader header, List<VHTBlock> blocks)
        {
            this.header = header;
            
            stream.SetLength(header.AllocatedSpace);
            stream.Position = 0;
            stream.Write(header.GetBytes(), 0, header.BlockSize);

            foreach (VHTBlock block in blocks)
            {
                stream.Position = block.Offset;
                stream.Write(block.GetBytes(header), 0, header.BlockSize);
            }

            stream.Flush();
        }

        //todo: use better hash
        internal uint GetHash(byte[] value)
        {
            uint res = 23;
            foreach (byte b in value)
            {
                res = res*31 + b;
            }
            return res;
        }

        public VHTTransaction BeginTransaction()
        {
            string walFilename = filename + "-wal";
            VHTWriteAheadLog wal = new VHTWriteAheadLog(this, walFilename);
            return new VHTTransaction(wal);
        }
    }
}