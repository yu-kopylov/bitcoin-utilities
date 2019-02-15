using System;
using System.Collections.Generic;
using System.Linq;

namespace BitcoinUtilities.Node.Modules.Outputs
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

        internal void CheckFollows(UtxoHeader lastHeader)
        {
            if (Height == 0)
            {
                if (lastHeader != null)
                {
                    throw new InvalidOperationException("Cannot insert a root header into the UTXO storage, because it already has headers.");
                }
            }
            else
            {
                if (lastHeader == null || lastHeader.Height != Height - 1 || !lastHeader.Hash.SequenceEqual(ParentHash))
                {
                    throw new InvalidOperationException("The last header in the UTXO storage does not match the parent header of the update.");
                }
            }
        }
    }
}