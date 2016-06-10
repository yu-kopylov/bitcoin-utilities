using System;
using System.Collections.Generic;

namespace BitcoinUtilities.Collections
{
    public static class CollectionUtils
    {
        /// <summary>
        /// Splits a given collection to a list of smaller collections.
        /// <para/>
        /// The order of elements in the resulting collection matches the order of element in the original collection.
        /// </summary>
        /// <param name="source">The given collection to split.</param>
        /// <param name="partSize">The maximum size of each collection in the result.</param>
        /// <exception cref="ArgumentException">If source is null or batchSize is less than or equal to zero.</exception>
        public static List<List<T>> Split<T>(this IEnumerable<T> source, int partSize)
        {
            if (source == null)
            {
                throw new ArgumentException($"{nameof(source)} is null.", nameof(source));
            }
            if (partSize <= 0)
            {
                throw new ArgumentException($"{nameof(partSize)} should be greater than zero.", nameof(partSize));
            }

            List<List<T>> res = new List<List<T>>();

            List<T> currentPart = null;

            foreach (T element in source)
            {
                if (currentPart == null || currentPart.Count == partSize)
                {
                    currentPart = new List<T>();
                    res.Add(currentPart);
                }
                currentPart.Add(element);
            }

            return res;
        }
    }
}