using System.Collections.Generic;

namespace BitcoinUtilities
{
    //todo: write test
    public class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        private static readonly ByteArrayComparer instance = new ByteArrayComparer();

        private ByteArrayComparer()
        {
        }

        public static ByteArrayComparer Instance
        {
            get { return instance; }
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
            int hash = 0;
            for (int i = 0; i < obj.Length; i++)
            {
                hash = hash*37 + obj[i];
            }
            return hash;
        }
    }
}