using System;
using System.Collections.Generic;
using System.Threading;

namespace BitcoinUtilities.Threading
{
    public class ConcurrentEnumerator<T> : IConcurrentEnumerator<T>
    {
        private readonly IReadOnlyList<T> collection;
        private int prevIndex = -1;

        public ConcurrentEnumerator(IReadOnlyList<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection), $"The given {nameof(collection)} is null.");
            }

            this.collection = collection;
        }

        public bool GetNext(out T value)
        {
            int index = Interlocked.Increment(ref prevIndex);
            if (index < collection.Count)
            {
                value = collection[index];
                return true;
            }

            // prevents prevIndex from growing indefinitely
            Interlocked.CompareExchange(ref prevIndex, collection.Count, index);

            value = default(T);
            return false;
        }
    }
}