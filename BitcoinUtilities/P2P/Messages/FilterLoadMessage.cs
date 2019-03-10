namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    /// The filterload message tells the receiving peer to filter all relayed transactions and requested merkle blocks through the provided filter.
    /// This allows clients to receive transactions relevant to their wallet plus a configurable rate of false positive transactions which can provide plausible-deniability privacy.
    /// </summary>
    /// <remarks>
    /// Specification: https://bitcoin.org/en/developer-reference#filterload
    /// </remarks>
    public class FilterLoadMessage : IBitcoinMessage
    {
        public const string Command = "filterload";
        public const int MaxFilterSize = 36000;

        public FilterLoadMessage(byte[] filter, uint functionCount, uint tweak, byte flags)
        {
            Filter = filter;
            FunctionCount = functionCount;
            Tweak = tweak;
            Flags = flags;
        }

        /// <summary>
        /// A bit field of arbitrary byte-aligned size. The maximum size is 36,000 bytes.
        /// </summary>
        public byte[] Filter { get; }

        /// <summary>
        /// The number of hash functions to use in this filter. The maximum value allowed in this field is 50.
        /// </summary>
        public uint FunctionCount { get; }

        /// <summary>
        /// An arbitrary value to add to the seed value in the hash function used by the bloom filter.
        /// </summary>
        public uint Tweak { get; }

        /// <summary>
        /// A set of flags that control how outpoints corresponding to a matched pubkey script are added to the filter.
        /// </summary>
        public byte Flags { get; }

        string IBitcoinMessage.Command
        {
            get { return Command; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
            writer.WriteArray(Filter, (w, b) => w.Write(b));
            writer.Write(FunctionCount);
            writer.Write(Tweak);
            writer.Write(Flags);
        }

        public static FilterLoadMessage Read(BitcoinStreamReader reader)
        {
            byte[] filter = reader.ReadArray(MaxFilterSize, r => r.ReadByte());
            uint functionCount = reader.ReadUInt32();
            uint tweak = reader.ReadUInt32();
            byte flags = reader.ReadByte();
            return new FilterLoadMessage(filter, functionCount, tweak, flags);
        }
    }
}