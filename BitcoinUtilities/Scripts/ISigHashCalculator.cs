namespace BitcoinUtilities.Scripts
{
    /// <summary>
    /// Calculates digest for a spending transaction.
    /// </summary>
    public interface ISigHashCalculator
    {
        /// <summary>
        /// Index of the output from the previous transaction.
        /// </summary>
        int InputIndex { get; set; }

        /// <summary>
        /// The amount of satoshis from the previous transaction output.
        /// </summary>
        ulong Amount { get; set; }

        /// <summary>
        /// Calculates digest for the spending transaction.
        /// </summary>
        /// <param name="sigHashType">Indicates which part of the spending transaction should be signed.</param>
        /// <param name="subScript">
        /// A script to include into the digest.
        /// <para/>
        /// Usually, it matches pubkey-script from the output of the previous transaction.
        /// </param>
        /// <returns>The digest of the spending transaction to be signed.</returns>
        byte[] Calculate(SigHashType sigHashType, byte[] subScript);
    }
}