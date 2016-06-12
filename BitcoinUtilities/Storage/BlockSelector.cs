namespace BitcoinUtilities.Storage
{
    public class BlockSelector
    {
        public enum SortOrder
        {
            Height,
            TotalWork
        }

        public enum SortDirection
        {
            Acs,
            Desc
        }

        public bool? HasContent { get; set; }
        public bool? IsInBestHeaderChain { get; set; }
        public bool? IsInBestBlockChain { get; set; }
        public SortOrder? Order { get; set; }
        public SortDirection? Direction { get; set; }

        /// <summary>
        /// Returns a new selector for the blocks with maximum height in the best header chain.
        /// </summary>
        public static BlockSelector LastBestHeader => new BlockSelector
        {
            IsInBestHeaderChain = true,
            Order = SortOrder.Height,
            Direction = SortDirection.Desc
        };
    }
}