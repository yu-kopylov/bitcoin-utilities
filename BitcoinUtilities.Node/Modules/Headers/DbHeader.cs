using System;
using System.Collections.Generic;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Modules.Headers
{
    public class DbHeader : IValidatableHeader
    {
        public static IComparer<DbHeader> HeightHashComparer { get; } = new HeightHashComparerImpl();

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

        public uint Timestamp => Header.Timestamp;
        public uint NBits => Header.NBits;
        public byte[] ParentHash => Header.PrevBlock;

        public DbHeader AppendAfter(DbHeader parentHeader, double work)
        {
            return new DbHeader(Header, Hash, parentHeader.Height + 1, parentHeader.TotalWork + work, IsValid && parentHeader.IsValid);
        }

        public DbHeader MarkInvalid()
        {
            if (!IsValid)
            {
                return this;
            }

            return new DbHeader(Header, Hash, Height, TotalWork, false);
        }

        public static DbHeader BestOf(IEnumerable<DbHeader> headers)
        {
            DbHeader bestHeader = null;
            foreach (var header in headers)
            {
                if (bestHeader == null || header.IsBetterThan(bestHeader))
                {
                    bestHeader = header;
                }
            }

            return bestHeader;
        }

        public bool IsBetterThan(DbHeader other)
        {
            int r = this.IsValid.CompareTo(other.IsValid);

            if (r == 0)
            {
                r = this.TotalWork.CompareTo(other.TotalWork);
            }

            if (r == 0)
            {
                r = -this.Timestamp.CompareTo(other.Timestamp);
            }

            return r > 0;
        }

        private class HeightHashComparerImpl : IComparer<DbHeader>
        {
            static readonly ByteArrayComparer arrayComparer = ByteArrayComparer.Instance;

            public int Compare(DbHeader x, DbHeader y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                int r = x.Height.CompareTo(y.Height);
                if (r == 0)
                {
                    r = arrayComparer.Compare(x.Hash, y.Hash);
                }

                return r;
            }
        }
    }
}