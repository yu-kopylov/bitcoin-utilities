using System.Collections.Generic;

namespace BitcoinUtilities.Node.Services.Outputs
{
    public class UtxoUpdate
    {
        public UtxoUpdate(int height, byte[] headerHash, byte[] parentHash)
        {
            Height = height;
            HeaderHash = headerHash;
            ParentHash = parentHash;
        }

        public int Height { get; }
        public byte[] HeaderHash { get; }
        public byte[] ParentHash { get; }
        public List<UtxoOperation> Operations { get; } = new List<UtxoOperation>();
    }
}