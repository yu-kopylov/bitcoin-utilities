namespace BitcoinUtilities.Node.Services.Outputs
{
    public class UtxoHeader
    {
        public UtxoHeader(byte[] hash, int height, bool isReversible)
        {
            Hash = hash;
            Height = height;
            IsReversible = isReversible;
        }

        public byte[] Hash { get; }
        public int Height { get; }
        public bool IsReversible { get; }
    }
}