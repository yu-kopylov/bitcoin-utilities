﻿using System;
using System.Collections.Generic;
using System.IO;

namespace BitcoinUtilities.Collections.VirtualDictionaryInternals
{
    internal class AtomicStream : Stream
    {
        private const int MaxUpdateSize = 1024*1024;
        private bool flushToDisk = true;

        private readonly Stream mainStream;
        private readonly Stream walStream;
        private readonly BinaryWriter walWriter;

        private readonly List<StreamUpdate> updates = new List<StreamUpdate>();
        private int flushedUpdateCount = 0;

        private readonly MemoryStream writeBuffer = new MemoryStream();
        private long writeBufferOffset = -1;

        private long virtualLength;
        private long virtualPosition;
        private bool dirty;

        public AtomicStream(Stream mainStream, Stream walStream)
        {
            if (!mainStream.CanSeek || !mainStream.CanRead || !mainStream.CanWrite)
            {
                //todo: describe exception
                throw new ArgumentException("mainStream should support read, write and seek.", "mainStream");
            }
            if (!walStream.CanSeek || !walStream.CanRead || !walStream.CanWrite)
            {
                //todo: describe exception
                throw new ArgumentException("walStream should support read, write and seek.", "walStream");
            }

            this.mainStream = mainStream;
            this.walStream = walStream;

            walWriter = new BinaryWriter(walStream);

            //todo: read WAL to determine real length
            virtualLength = mainStream.Length;
            virtualPosition = 0;
        }

        public bool FlushToDisk
        {
            get { return flushToDisk; }
            set { flushToDisk = value; }
        }

        public void Commit()
        {
            FinalizeWriteUpdate();

            updates.Add(StreamUpdate.Commit());

            walStream.Position = 0;

            //todo: write header and transaction number

            for (; flushedUpdateCount < updates.Count; flushedUpdateCount++)
            {
                SaveUpdate(updates[flushedUpdateCount]);
            }

            Flush(walStream);

            ApplyUpdates();

            Flush(mainStream);

            updates.Clear();
            flushedUpdateCount = 0;
            dirty = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //todo: Flush, Commit or Rollback?
            }
        }

        public override void Flush()
        {
            //todo: is it usefull in AtomicStream?
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                virtualPosition = offset;
            }
            else if (origin == SeekOrigin.End)
            {
                //todo: is this correct?
                virtualLength += offset;
            }
            else if (origin == SeekOrigin.Current)
            {
                virtualPosition += offset;
            }
            //todo: compare with length?
            return virtualPosition;
        }

        public override void SetLength(long value)
        {
            if (value < virtualLength)
            {
                //todo: specify exception
                throw new Exception("AtomicStream cannot be truncated.");
            }

            FinalizeWriteUpdate();

            updates.Add(StreamUpdate.SetLength(value));

            virtualLength = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            //todo: set correct condition
            if (dirty)
            {
                //todo: specify exception
                throw new Exception("Cannot read modified but uncommited stream.");
            }
            //todo: does this comparison improve efficiency?
            if (mainStream.Position != virtualPosition)
            {
                mainStream.Position = virtualPosition;
            }

            int bytesRead = mainStream.Read(buffer, offset, count);
            virtualPosition += bytesRead;

            //todo: this approach does not look very reliable
            if (bytesRead < count && virtualPosition < virtualLength)
            {
                throw new Exception("Attempt to read allocated but unwritten data.");
            }

            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            //todo: test all branches
            if (writeBufferOffset >= 0)
            {
                if (virtualPosition < writeBufferOffset ||
                    virtualPosition > writeBufferOffset + writeBuffer.Length + 1 ||
                    virtualPosition + count - writeBufferOffset > MaxUpdateSize)
                {
                    FinalizeWriteUpdate();
                    writeBufferOffset = virtualPosition;
                }
                else
                {
                    writeBuffer.Position = virtualPosition - writeBufferOffset;
                }
            }
            else
            {
                writeBufferOffset = virtualPosition;
            }

            writeBuffer.Write(buffer, offset, count);

            if (writeBuffer.Length >= MaxUpdateSize)
            {
                FinalizeWriteUpdate();
            }

            virtualPosition += count;
            dirty = true;

            if (virtualPosition > virtualLength)
            {
                virtualLength = virtualPosition;
            }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return virtualLength; }
        }

        public override long Position
        {
            get { return virtualPosition; }
            set { virtualPosition = value; }
        }

        private void FinalizeWriteUpdate()
        {
            if (writeBufferOffset < 0)
            {
                return;
            }
            byte[] data = writeBuffer.ToArray();
            updates.Add(StreamUpdate.Write(writeBufferOffset, data));

            writeBuffer.SetLength(0);
            writeBufferOffset = -1;
        }

        private void SaveUpdate(StreamUpdate update)
        {
            walWriter.Write((uint) update.UpdateType);
            walWriter.Write(update.Value);
            if (update.Data != null)
            {
                walWriter.Write(update.Data, 0, update.Data.Length);
            }
        }

        private void ApplyUpdates()
        {
            foreach (StreamUpdate update in updates)
            {
                ApplyUpdate(update);
            }
        }

        private void ApplyUpdate(StreamUpdate update)
        {
            if (update.UpdateType == StreamUpdateType.Write)
            {
                mainStream.Position = update.Value;
                mainStream.Write(update.Data, 0, update.Data.Length);
            }
            else if (update.UpdateType == StreamUpdateType.SetLength)
            {
                mainStream.SetLength(update.Value);
            }
            else if (update.UpdateType == StreamUpdateType.Commit)
            {
                // nothing to do, we are commiting changes already
            }
            else
            {
                throw new Exception(string.Format("Unexpected StreamUpdateType: {0}", update.UpdateType));
            }
        }

        private void Flush(Stream stream)
        {
            if (flushToDisk && stream is FileStream)
            {
                FileStream fileStream = (FileStream) stream;
                fileStream.Flush(true);
            }
            else
            {
                stream.Flush();
            }
        }

        private enum StreamUpdateType
        {
            Write = 1,
            SetLength = 2,
            Commit = 3
        }

        private struct StreamUpdate
        {
            private readonly StreamUpdateType updateType;
            private readonly long value;
            private readonly byte[] data;

            private StreamUpdate(StreamUpdateType updateType, long value, byte[] data)
            {
                this.updateType = updateType;
                this.value = value;
                this.data = data;
            }

            public static StreamUpdate SetLength(long value)
            {
                return new StreamUpdate(StreamUpdateType.SetLength, value, null);
            }

            public static StreamUpdate Write(long offset, byte[] data)
            {
                return new StreamUpdate(StreamUpdateType.Write, offset, data);
            }

            public static StreamUpdate Commit()
            {
                return new StreamUpdate(StreamUpdateType.Commit, 0, null);
            }

            public StreamUpdateType UpdateType
            {
                get { return updateType; }
            }

            public long Value
            {
                get { return value; }
            }

            public byte[] Data
            {
                get { return data; }
            }
        }
    }
}