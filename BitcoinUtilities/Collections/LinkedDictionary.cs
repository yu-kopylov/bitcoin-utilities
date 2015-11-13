using System;
using System.Collections;
using System.Collections.Generic;

namespace BitcoinUtilities.Collections
{
    /// <summary>
    /// Represents a collection of keys and values.
    /// <para/>
    /// Guratees that order of enumeration is the same order they were added to the dictionary.
    /// </summary>
    public class LinkedDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> dict;
        private readonly LinkedList<KeyValuePair<TKey, TValue>> list;

        /// <summary>
        /// Initializes a new instance of the dictionary that uses the default equality comparer.
        /// </summary>
        public LinkedDictionary()
        {
            dict = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>();
            list = new LinkedList<KeyValuePair<TKey, TValue>>();
        }

        /// <summary>
        /// Initializes a new instance of the dictionary that uses the specified equality comparer.
        /// </summary>
        public LinkedDictionary(IEqualityComparer<TKey> keyComparer)
        {
            dict = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(keyComparer);
            list = new LinkedList<KeyValuePair<TKey, TValue>>();
        }

        /// <summary>
        /// Gets the number of elements in the dictionary.
        /// </summary>
        public int Count
        {
            get { return list.Count; }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// <para/>
        /// Does not change the order of enumeration.
        /// </summary>
        /// <exception cref="ArgumentNullException">The given key is null.</exception>
        /// <exception cref="KeyNotFoundException">The given key does not exist in the collection.</exception>
        public TValue this[TKey key]
        {
            get { return dict[key].Value.Value; }
            set
            {
                LinkedListNode<KeyValuePair<TKey, TValue>> node;
                if (dict.TryGetValue(key, out node))
                {
                    node.Value = new KeyValuePair<TKey, TValue>(node.Value.Key, value);
                    return;
                }
                node = list.AddLast(new KeyValuePair<TKey, TValue>(key, value));
                dict.Add(key, node);
            }
        }

        /// <summary>
        /// Checks if the dictionary contains the specified key.
        /// </summary>
        /// <param name="key">The key to look in the dictionary.</param>
        /// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
        public bool ContainsKey(TKey key)
        {
            return dict.ContainsKey(key);
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="ArgumentException">An element with the same key already exists in the dictionary.</exception>
        public void Add(TKey key, TValue value)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> node = new LinkedListNode<KeyValuePair<TKey, TValue>>(new KeyValuePair<TKey, TValue>(key, value));
            dict.Add(key, node);
            list.AddLast(node);
        }

        /// <summary>
        /// Removes the value with the specified key from the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>true if the element is found and removed; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        public bool Remove(TKey key)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> node;
            if (!dict.TryGetValue(key, out node))
            {
                return false;
            }
            dict.Remove(key);
            list.Remove(node);
            return true;
        }

        /// <summary>
        /// Removes all elements from the dictionary.
        /// </summary>
        public void Clear()
        {
            dict.Clear();
            list.Clear();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the dictionary.
        /// <para/>
        /// Elements are enumerated in the same order they were added to the dictionary.
        /// </summary>
        /// <returns>An enumerator for the dictionary.</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the dictionary.
        /// <para/>
        /// Elements are enumerated in the same order they were added to the dictionary.
        /// </summary>
        /// <returns>An enumerator for the dictionary.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}