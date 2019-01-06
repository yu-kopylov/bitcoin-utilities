using System;
using System.Collections;
using System.Collections.Generic;
using BitcoinUtilities.Node.Rules;

namespace BitcoinUtilities.Node.Services.Headers
{
    public class HeaderSubChain : ISubchain<DbHeader>, IReadOnlyCollection<DbHeader>
    {
        private readonly List<DbHeader> headers;

        private DbHeader head;

        public HeaderSubChain(IReadOnlyCollection<DbHeader> headers)
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
            return headers[height - headers[0].Height];
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
    }
}