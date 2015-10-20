namespace BitcoinUtilities.Storage.Models
{
    public enum PubkeyScriptType
    {
        /// <summary>
        /// The PubkeyScript field conatins a full script.
        /// The PublicKey field is null. 
        /// </summary>
        Plain,

        /// <summary>
        /// Public key has a form: (OP:[0x01-0x4b]) (public key) (OP_CHECKSIG: 0xAC)
        /// The PubkeyScript field conatins OP.
        /// The PublicKey field contains the public key. 
        /// </summary>
        PublicKey
    }
}
