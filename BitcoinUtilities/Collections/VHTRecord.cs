namespace BitcoinUtilities.Collections
{
    //todo: class or struct?
    internal class VHTRecord
    {
        public uint Hash { get; set; }
        public byte[] Key { get; set; }
        public byte[] Value { get; set; }
    }
}