using System;
using System.Collections.Generic;

namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    /// Return a headers packet containing the headers of blocks starting right after the last known hash in the block locator object, up to <see cref="HashStop"/> or 2000 blocks, whichever comes first.
    /// To receive the next block headers, one needs to issue getheaders again with a new block locator object.
    /// <para/>
    /// The getheaders command is used by thin clients to quickly download the block chain where the contents of the transactions would be irrelevant (because they are not ours).
    /// <para/>
    /// Keep in mind that some clients may provide headers of blocks which are invalid if the block locator object contains a hash on the invalid branch.
    /// </summary>
    public class GetHeadersMessage : IBitcoinMessage
    {
        public const string Command = "getheaders";

        /// <summary>
        /// The maximum number of entries in a locator.
        /// </summary>
        /// <remarks>
        /// Introduced in Bitcoin Core v0.17.0.
        /// </remarks>
        public const int MaxLocatorSize = 101;

        private readonly int protocolVersion;
        private readonly List<byte[]> locatorHashes;
        private readonly byte[] hashStop;

        public GetHeadersMessage(int protocolVersion, byte[][] locatorHashes, byte[] hashStop)
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
        /// hash of the last desired block header; set to zero to get as many blocks as possible (2000)
        /// </summary>
        public byte[] HashStop
        {
            get { return hashStop; }
        }

        string IBitcoinMessage.Command
        {
            get { return Command; }
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

        public static GetHeadersMessage Read(BitcoinStreamReader reader)
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

            return new GetHeadersMessage(protocolVersion, locatorHashes, hashStop);
        }
    }
}