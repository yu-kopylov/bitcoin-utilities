using System;
using System.Collections.Generic;

namespace BitcoinUtilities
{
    //todo: write test
    public class ByteArrayComparer : IEqualityComparer<byte[]>, IComparer<byte[]>
    {
        public static ByteArrayComparer Instance { get; } = new ByteArrayComparer();

        private ByteArrayComparer()
        {
        }

        public bool Equals(byte[] x, byte[] y)
        {
            if (x == y)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            if (x.Length != y.Length)
            {
                return false;
            }

            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(byte[] obj)
        {
            if (obj == null)
            {
                return 0;
            }

            int hash = 23;
            for (int i = 0; i < obj.Length; i++)
            {
                hash = hash * 31 + obj[i];
            }

            return hash;
        }

        public int Compare(byte[] x, byte[] y)
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

            int minLength = Math.Min(x.Length, y.Length);
            for (int i = 0; i < minLength; i++)
            {
                int r = x[i].CompareTo(y[i]);
                if (r != 0)
                {
                    return r;
                }
            }

            return x.Length.CompareTo(y.Length);
        }
    }
}