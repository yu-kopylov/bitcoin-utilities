namespace BitcoinUtilities.Node.Rules
{
    /// <summary>
    /// A header that can be validated by <see cref="BlockHeaderValidator"/>.
    /// </summary>
    public interface IValidatableHeader
    {
        byte[] Hash { get; }
        int Height { get; }
        uint Timestamp { get; }
        uint NBits { get; }
    }
}