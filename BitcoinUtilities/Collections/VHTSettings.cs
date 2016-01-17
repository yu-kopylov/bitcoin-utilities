namespace BitcoinUtilities.Collections
{
    //todo: add XMLDOC
    public class VHTSettings
    {
        /// <summary>
        /// The name of the file in which the hash table will be stored.
        /// </summary>
        public string Filename { get; set; }

        public int KeyLength { get; set; }
        public int ValueLength { get; set; }
    }
}