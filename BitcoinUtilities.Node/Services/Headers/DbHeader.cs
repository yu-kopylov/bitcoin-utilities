using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Services.Headers
{
    public class DbHeader
    {
        public DbHeader(BlockHeader header, byte[] hash, int height, double totalWork)
        {
            Header = header;
            Hash = hash;
            Height = height;
            TotalWork = totalWork;
        }

        public BlockHeader Header { get; }
        public byte[] Hash { get; }
        public int Height { get; }
        public double TotalWork { get; }
    }
}