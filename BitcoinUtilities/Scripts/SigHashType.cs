namespace BitcoinUtilities.Scripts
{
    /// <summary>
    /// A flag to Bitcoin signatures that indicates what parts of the transaction the signature signs.
    /// <para/>
    /// Specification: https://en.bitcoin.it/wiki/OP_CHECKSIG
    /// </summary>
    public enum SigHashType : byte
    {
        /// <summary>
        /// Sign all of the outputs.
        /// </summary>
        All = 0x01,

        /// <summary>
        /// Sign none of the outputs. Think as: I don't care where the bitcoins go.
        /// </summary>
        None = 0x02,

        /// <summary>
        /// Sign one of the outputs. Think as: I don't care where the other outputs go.
        /// </summary>
        Single = 0x03,

        /// <summary>
        /// Think as: Let other people add inputs to this transaction, I don't care where the rest of the bitcoins come from.
        /// </summary>
        AnyoneCanPay = 0x80
    }
}