using System;
using System.Collections.Generic;
using System.Linq;

namespace BitcoinUtilities.Node.Services.Outputs
{
    public class UtxoAggregateUpdate
    {
        public int FirstHeaderHeight { get; private set; }
        public byte[] FirstHeaderParent { get; private set; }

        public List<byte[]> HeaderHashes { get; } = new List<byte[]>();

        public List<UtxoOutput> AllSpentOutputs { get; } = new List<UtxoOutput>();
        public List<UtxoOutput> ExistingSpentOutputs { get; } = new List<UtxoOutput>();
        public Dictionary<byte[], UtxoOutput> UnspentOutputs { get; } = new Dictionary<byte[], UtxoOutput>(ByteArrayComparer.Instance);

        public void Add(UtxoUpdate update)
        {
            if (HeaderHashes.Count == 0)
            {
                FirstHeaderHeight = update.Height;
                FirstHeaderParent = update.ParentHash;
            }
            else
            {
                if (
                    update.Height != FirstHeaderHeight + HeaderHashes.Count ||
                    !ByteArrayComparer.Instance.Equals(update.ParentHash, HeaderHashes.Last())
                )
                {
                    throw new InvalidOperationException("UTXO update does not follow previous update.");
                }
            }

            HeaderHashes.Add(update.HeaderHash);

            foreach (UtxoOutput output in update.SpentOutputs)
            {
                // todo: this seems to be a wrong place to set heigh
                UtxoOutput spentOutput = output.Spend(update.Height);

                AllSpentOutputs.Add(spentOutput);

                if (!UnspentOutputs.Remove(output.OutputPoint))
                {
                    ExistingSpentOutputs.Add(spentOutput);
                }
            }

            foreach (UtxoOutput output in update.NewOutputs)
            {
                UnspentOutputs.Add(output.OutputPoint, output);
            }
        }
    }
}