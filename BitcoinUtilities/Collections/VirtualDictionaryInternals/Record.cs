namespace BitcoinUtilities.Collections.VirtualDictionaryInternals
{
    internal struct Record
    {
        private readonly ByteArrayRef key;
        private readonly ByteArrayRef value;

        public Record(ByteArrayRef key, ByteArrayRef value)
        {
            this.key = key;
            this.value = value;
        }

        public Record(byte[] key, byte[] value)
        {
            this.key = new ByteArrayRef(key, 0, key.Length);
            this.value = value == null ? new ByteArrayRef(null, 0, 0) : new ByteArrayRef(value, 0, value.Length);
        }

        public ByteArrayRef Key
        {
            get { return key; }
        }

        public ByteArrayRef Value
        {
            get { return value; }
        }
    }
}