using System;
using System.Collections.Generic;

namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    ///  Return an inv packet containing the list of blocks starting right after the last known hash in the block locator object, up to <see cref="HashStop"/> or 500 blocks, whichever comes first.
    /// <para/>
    /// The <see cref="LocatorHashes"/> are processed by a node in the order as they appear in the message.
    /// If a block hash is found in the node's main chain, the list of its children is returned back via the inv message and the remaining locators are ignored, no matter if the requested limit was reached, or not.
    /// <para/>
    /// To receive the next blocks hashes, one needs to issue getblocks again with a new block locator object.
    /// Keep in mind that some clients may provide blocks which are invalid if the block locator object contains a hash on the invalid branch.
    ///  </summary>
    public class GetBlocksMessage
    {
        public const string Command = "getblocks";

        private readonly int protocolVersion;
        private readonly List<byte[]> locatorHashes;
        private readonly byte[] hashStop;

        public GetBlocksMessage(int protocolVersion, byte[][] locatorHashes, byte[] hashStop)
        {
            this.locatorHashes = new List<byte[]>(locatorHashes);
            this.hashStop = hashStop;
            this.protocolVersion = protocolVersion;
        }

        /// <summary>
        /// the protocol version
        /// </summary>
        public int ProtocolVersion
        {
            get { return protocolVersion; }
        }

        /// <summary>
        /// block locator object; newest back to genesis block (dense to start, but then sparse)
        /// </summary>
        public List<byte[]> LocatorHashes
        {
            get { return locatorHashes; }
        }

        /// <summary>
        /// hash of the last desired block; set to zero to get as many blocks as possible (500)
        /// </summary>
        public byte[] HashStop
        {
            get { return hashStop; }
        }


        public void Write(BitcoinStreamWriter writer)
        {
            writer.Write(protocolVersion);
            writer.WriteCompact((ulong) locatorHashes.Count);
            foreach (byte[] hash in locatorHashes)
            {
                writer.Write(hash);
            }
            writer.Write(hashStop);
        }

        public static GetBlocksMessage Read(BitcoinStreamReader reader)
        {
            int protocolVersion = reader.ReadInt32();

            ulong count = reader.ReadUInt64Compact();
            if (count > 10000)
            {
                //todo: handle correctly
                throw new Exception("Too many locator hashes.");
            }
            
            byte[][] locatorHashes = new byte[count][];
            for (ulong i = 0; i < count; i++)
            {
                locatorHashes[i] = reader.ReadBytes(32);
            }

            byte[] hashStop = reader.ReadBytes(32);

            return new GetBlocksMessage(protocolVersion, locatorHashes, hashStop);
        }
    }
}