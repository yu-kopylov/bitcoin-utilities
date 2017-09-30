using System;

namespace BitcoinUtilities.Scripts
{
    /// <summary>
    /// A flag for Bitcoin signatures that indicates which parts of a transaction the signature signs.
    /// <para/>
    /// Specification: https://en.bitcoin.it/wiki/OP_CHECKSIG
    /// </summary>
    [Flags]
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
        /// Indicates that the Bitcoin Cash digest algorithm is used for signature.
        /// </summary>
        ForkId = 0x40,

        /// <summary>
        /// Think as: Let other people add inputs to this transaction, I don't care where the rest of the bitcoins come from.
        /// </summary>
        AnyoneCanPay = 0x80
    }
}