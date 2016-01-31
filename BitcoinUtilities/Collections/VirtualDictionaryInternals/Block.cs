using System.Collections.Generic;

namespace BitcoinUtilities.Collections.VirtualDictionaryInternals
{
    internal class Block
    {
        private readonly Record[] records;

        public Block(Record[] records)
        {
            this.records = records;
        }

        public Record[] Records
        {
            get { return records; }
        }
    }
}