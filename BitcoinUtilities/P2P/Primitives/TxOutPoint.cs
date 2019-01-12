using System;

namespace BitcoinUtilities.P2P.Primitives
{
    /// <summary>
    /// An output transaction reference.
    /// </summary>
    public struct TxOutPoint : IEquatable<TxOutPoint>
    {
        private readonly byte[] hash;
        private readonly int index;

        public TxOutPoint(byte[] hash, int index)
        {
            this.hash = hash;
            this.index = index;
        }

        /// <summary>
        /// The hash of the referenced transaction.
        /// </summary>
        public byte[] Hash
        {
            get { return hash; }
        }

        /// <summary>
        /// The index of the specific output in the transaction. The first output is 0, etc.
        /// </summary>
        public int Index
        {
            get { return index; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
            writer.Write(hash);
            writer.Write(index);
        }

        public static TxOutPoint Read(BitcoinStreamReader reader)
        {
            byte[] hash = reader.ReadBytes(32);
            int index = reader.ReadInt32();
            return new TxOutPoint(hash, index);
        }

        public bool Equals(TxOutPoint other)
        {
            return index == other.index && ByteArrayComparer.Instance.Equals(hash, other.hash);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is TxOutPoint point && Equals(point);
        }

        public override int GetHashCode()
        {
            return (ByteArrayComparer.Instance.GetHashCode(hash) * 397) ^ index;
        }

        public static bool operator ==(TxOutPoint left, TxOutPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TxOutPoint left, TxOutPoint right)
        {
            return !left.Equals(right);
        }
    }
}