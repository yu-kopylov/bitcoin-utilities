using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Services.Headers
{
    public class DbHeader
    {
        public DbHeader(BlockHeader header, byte[] hash, int height, double totalWork, bool isValid)
        {
            Header = header;
            Hash = hash;
            Height = height;
            TotalWork = totalWork;
            IsValid = isValid;
        }

        public BlockHeader Header { get; }
        public byte[] Hash { get; }
        public int Height { get; }
        public double TotalWork { get; }
        public bool IsValid { get; }

        public byte[] ParentHash => Header.PrevBlock;

        public DbHeader MarkInvalid()
        {
            if (!IsValid)
            {
                return this;
            }

            return new DbHeader(Header, Hash, Height, TotalWork, false);
        }

        public string AsText =>
            $"Hash:\t{HexUtils.GetString(this.Hash)}\r\n" +
            $"Height:\t{this.Height}\r\n" +
            $"TotalWork:\t{this.TotalWork}\r\n" +
            $"IsValid:\t{this.IsValid}\r\n" +
            $"Header:\t{HexUtils.GetString(BitcoinStreamWriter.GetBytes(this.Header.Write))}";
    }
}