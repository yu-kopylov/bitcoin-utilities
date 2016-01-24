using System.Collections.Generic;

namespace BitcoinUtilities.Collections.VirtualDictionaryInternals
{
    internal class Block
    {
        private List<Record> records = new List<Record>();

        public List<Record> Records
        {
            get { return records; }
            set { records = value; }
        }
    }
}