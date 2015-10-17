namespace BitcoinUtilities.P2P.Primitives
{
    public enum InventoryVectorType
    {
        /// <summary>
        /// Any data of with this number may be ignored.
        /// </summary>
        Error = 0,

        /// <summary>
        /// Hash is related to a transaction.
        /// </summary>
        MsgTx = 1,

        /// <summary>
        /// Hash is related to a data block.
        /// </summary>
        MsgBlock = 2,

        /// <summary>
        /// Hash of a block header; identical to MSG_BLOCK. When used in a getdata message, this indicates the reply should be a merkleblock message rather than a block message; this only works if a bloom filter has been set.
        /// </summary>
        MsgFilteredBlock = 3,
    }
}