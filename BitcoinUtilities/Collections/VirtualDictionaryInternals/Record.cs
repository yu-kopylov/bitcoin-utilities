namespace BitcoinUtilities.Collections.VirtualDictionaryInternals
{
    internal class Record
    {
        private readonly byte[] key;
        private readonly byte[] value;

        public Record(byte[] key, byte[] value)
        {
            this.key = key;
            this.value = value;
        }

        public byte[] Key
        {
            get { return key; }
        }

        public byte[] Value
        {
            get { return value; }
        }
    }
}