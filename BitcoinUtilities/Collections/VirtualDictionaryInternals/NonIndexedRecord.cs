namespace BitcoinUtilities.Collections.VirtualDictionaryInternals
{
    //todo: class or struct?
    internal class NonIndexedRecord
    {
        private readonly byte[] key;
        private readonly byte[] value;

        public NonIndexedRecord(byte[] key, byte[] value)
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