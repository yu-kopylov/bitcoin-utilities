using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    /// The headers packet returns block headers in response to a <see cref="GetHeadersMessage"/> packet.
    /// </summary>
    public class HeadersMessage : IBitcoinMessage
    {
        public const string Command = "headers";

        private const int MaxHeadersPerMessage = 2000;

        private readonly BlockHeader[] headers;

        public HeadersMessage(BlockHeader[] headers)
        {
            this.headers = headers;
        }

        /// <summary>
        /// Block headers.
        /// </summary>
        public BlockHeader[] Headers
        {
            get { return headers; }
        }

        string IBitcoinMessage.Command
        {
            get { return Command; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
            writer.WriteArray(headers, (w, h) =>
            {
                h.Write(w);
                w.WriteCompact(0);
            });
        }

        public static HeadersMessage Read(BitcoinStreamReader reader)
        {
            BlockHeader[] headers = reader.ReadArray(MaxHeadersPerMessage, r =>
            {
                BlockHeader header = BlockHeader.Read(r);
                ulong txCount = r.ReadUInt64Compact();
                if (txCount != 0)
                {
                    throw new BitcoinNetworkException($"Invalid transaction count in {Command} message: {txCount}.");
                }
                return header;
            });
            return new HeadersMessage(headers);
        }
    }
}