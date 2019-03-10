namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    /// <para>
    /// The filterclear message tells the receiving peer to remove a previously-set bloom filter.
    /// This also undoes the effect of setting the relay field in the version message to 0, allowing unfiltered access to inv messages announcing new transactions.
    /// </para>
    /// <para>
    /// Bitcoin Core does not require a filterclear message before a replacement filter is loaded with filterload.
    /// It also doesn't require a filterload message before a filterclear message.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Specification: https://bitcoin.org/en/developer-reference#filterclear
    /// </remarks>
    public class FilterClearMessage : IBitcoinMessage
    {
        public const string Command = "filterclear";

        string IBitcoinMessage.Command
        {
            get { return Command; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
        }

        public static FilterClearMessage Read(BitcoinStreamReader reader)
        {
            return new FilterClearMessage();
        }
    }
}