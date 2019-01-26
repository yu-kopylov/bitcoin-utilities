namespace BitcoinUtilities.Threading
{
    /// <summary>
    /// A thread-safe enumerator over a collection.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    public interface IConcurrentEnumerator<T>
    {
        /// <summary>
        /// Obtains the next value from the collection.
        /// </summary>
        /// <param name="value">The value obtained from the collection. Contains the default value of <typeparamref name="T"/> if end of the collection is reached</param>
        /// <returns>true if value was successfully obtained from the collection; false if end of the collection is reached.</returns>
        bool GetNext(out T value);
    }
}