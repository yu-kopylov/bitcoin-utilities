using System;
using System.Collections;
using System.Collections.Generic;
using BitcoinUtilities.Node.Rules;

namespace BitcoinUtilities.Node.Modules.Headers
{
    public class HeaderSubChain : ISubchain<DbHeader>, IReadOnlyCollection<DbHeader>
    {
        private readonly List<DbHeader> headers;

        private DbHeader head;

        public HeaderSubChain(IEnumerable<DbHeader> headers)
        {
            this.headers = new List<DbHeader>(headers);
            this.head = this.headers[this.headers.Count - 1];
        }

        public DbHeader Head => head;

        public void Append(DbHeader header)
        {
            if (!ByteArrayComparer.Instance.Equals(head.Hash, header.ParentHash))
            {
                throw new ArgumentException("The given header is not a direct child of current head.", nameof(header));
            }

            headers.Add(header);
            head = header;
        }

        public int Count => headers.Count;

        public DbHeader GetBlockByHeight(int height)
        {
            int index = GetIndexFromHeight(height);
            return headers[index];
        }

        public DbHeader GetBlockByOffset(int offset)
        {
            return headers[headers.Count - 1 - offset];
        }

        public IEnumerator<DbHeader> GetEnumerator()
        {
            return headers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return headers.GetEnumerator();
        }

        /// <summary>
        /// <para>Returns a chain of headers starting at the header with the given height.</para>
        /// <para>The returned chain can be shorter than the given length in case of the chain ending at the last header of this chain.</para>
        /// </summary>
        /// <param name="firstHeaderHeight">The height of the first header.</param>
        /// <param name="length">The expected length.</param>
        /// <exception cref="ArgumentException">This chain does not have a header with the given height.</exception>
        /// <returns>The requested subchain.</returns>
        public HeaderSubChain GetChildSubChain(int firstHeaderHeight, int length)
        {
            int index = GetIndexFromHeight(firstHeaderHeight);

            if (index < 0 || index >= headers.Count)
            {
                throw new ArgumentException("This chain does not have a header with the given height.", nameof(firstHeaderHeight));
            }

            int availableLength = headers.Count - index;

            return new HeaderSubChain(headers.GetRange(index, availableLength < length ? availableLength : length));
        }

        private int GetIndexFromHeight(int height)
        {
            return height - headers[0].Height;
        }
    }
}