using System;
using System.Collections.Generic;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    /// The merkleblock message is a reply to a <see cref="GetDataMessage"/> which requested a block using the inventory type <see cref="InventoryVectorType.MsgFilteredBlock"/>.
    /// <para/>
    /// It is only part of the reply: if any matching transactions are found, they will be sent separately as <see cref="TxMessage"/>.
    /// </summary>
    public class MerkleBlockMessage : IBitcoinMessage
    {
        public const string Command = "merkleblock";

        private readonly BlockHeader blockHeader;
        private readonly uint totalTransactions;
        private readonly List<byte[]> hashes;
        private readonly byte[] flags;

        private MerkleBlockMessage(BlockHeader blockHeader, uint totalTransactions, byte[][] hashes, byte[] flags)
        {
            this.blockHeader = blockHeader;
            this.totalTransactions = totalTransactions;
            this.hashes = new List<byte[]>(hashes);
            this.flags = flags;
        }

        /// <summary>
        /// The block header.
        /// </summary>
        public BlockHeader BlockHeader
        {
            get { return blockHeader; }
        }

        /// <summary>
        /// The number of transactions in the block (including ones that don’t match the filter).
        /// </summary>
        public uint TotalTransactions
        {
            get { return totalTransactions; }
        }

        /// <summary>
        /// One or more hashes of both transactions and merkle nodes in internal byte order.
        /// <para/>
        /// Each hash is 32 bits.
        /// </summary>
        public List<byte[]> Hashes
        {
            get { return hashes; }
        }

        /// <summary>
        /// A sequence of bits packed eight in a byte with the least significant bit first.
        /// <para/>
        /// May be padded to the nearest byte boundary but must not contain any more bits than that.
        /// <para/>
        /// Used to assign the hashes to particular nodes in the merkle tree.
        /// </summary>
        public byte[] Flags
        {
            get { return flags; }
        }

        string IBitcoinMessage.Command
        {
            get { return Command; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
            blockHeader.Write(writer);
            writer.Write(totalTransactions);
            writer.WriteCompact((ulong) hashes.Count);
            foreach (byte[] hash in hashes)
            {
                writer.Write(hash);
            }
            writer.WriteCompact((ulong) flags.Length);
            writer.Write(flags);
        }

        public static MerkleBlockMessage Read(BitcoinStreamReader reader)
        {
            BlockHeader blockHeader = BlockHeader.Read(reader);
            uint totalTransactions = reader.ReadUInt32();

            ulong hashesCount = reader.ReadUInt64Compact();
            if (hashesCount > 1024*1024) //todo: see if there is actual limitation for this field 
            {
                //todo: handle correctly
                throw new Exception("Too many hashes.");
            }

            byte[][] hashes = new byte[hashesCount][];
            for (ulong i = 0; i < hashesCount; i++)
            {
                hashes[i] = reader.ReadBytes(32);
            }

            ulong flagBytesCount = reader.ReadUInt64Compact();
            if (flagBytesCount > 1024*1024) //todo: see if there is actual limitation for this field 
            {
                //todo: handle correctly
                throw new Exception("Too many flags.");
            }
            byte[] flags = reader.ReadBytes((int) flagBytesCount);

            return new MerkleBlockMessage(blockHeader, totalTransactions, hashes, flags);
        }
    }
}