namespace BitcoinUtilities
{
    /// <summary>
    /// Indicates blockchain version after hard fork.
    /// </summary>
    public enum BitcoinFork
    {
        /// <summary>
        /// Bitcoin Core
        /// </summary>
        Core,

        /// <summary>
        /// Bitcoin Cash
        /// <para/>
        /// Separated from Bitcoin Core on 01/08/2017. Last common block height 478558.
        /// </summary>
        Cash
    }
}