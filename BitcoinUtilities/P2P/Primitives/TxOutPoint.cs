using System;

namespace BitcoinUtilities.P2P.Primitives
{
    /// <summary>
    /// An output transaction reference.
    /// </summary>
    public struct TxOutPoint : IEquatable<TxOutPoint>
    {
        public TxOutPoint(byte[] hash, int index)
        {
            Hash = hash;
            Index = index;
        }

        /// <summary>
        /// The hash of the referenced transaction.
        /// </summary>
        public byte[] Hash { get; }

        /// <summary>
        /// The index of the specific output in the transaction. The first output is 0, etc.
        /// </summary>
        public int Index { get; }

        public void Write(BitcoinStreamWriter writer)
        {
            writer.Write(Hash);
            writer.Write(Index);
        }

        public static TxOutPoint Read(BitcoinStreamReader reader)
        {
            byte[] hash = reader.ReadBytes(32);
            int index = reader.ReadInt32();
            return new TxOutPoint(hash, index);
        }

        public bool Equals(TxOutPoint other)
        {
            return Index == other.Index && ByteArrayComparer.Instance.Equals(Hash, other.Hash);
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
            return (ByteArrayComparer.Instance.GetHashCode(Hash) * 397) ^ Index;
        }

        public static bool operator ==(TxOutPoint left, TxOutPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TxOutPoint left, TxOutPoint right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return HexUtils.GetReversedString(Hash) + ":" + Index;
        }
    }
}