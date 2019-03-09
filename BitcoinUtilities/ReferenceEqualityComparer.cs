using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BitcoinUtilities
{
    public class ReferenceEqualityComparer<T> : IEqualityComparer<T>
    {
        public static ReferenceEqualityComparer<T> Instance { get; } = new ReferenceEqualityComparer<T>();

        private ReferenceEqualityComparer()
        {
        }

        public bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}