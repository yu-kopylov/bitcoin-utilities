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

        public List<Tuple<int, List<UtxoOutput>>> ExistingSpentOutputs { get; } = new List<Tuple<int, List<UtxoOutput>>>();
        public List<UtxoOutput> DirectlySpentOutputs { get; } = new List<UtxoOutput>();
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

            List<UtxoOutput> existingSpentOutputs = new List<UtxoOutput>();
            ExistingSpentOutputs.Add(new Tuple<int, List<UtxoOutput>>(update.Height, existingSpentOutputs));

            foreach (UtxoOutput output in update.SpentOutputs)
            {
                if (UnspentOutputs.Remove(output.OutputPoint))
                {
                    DirectlySpentOutputs.Add(output.Spend(update.Height));
                }
                else
                {
                    existingSpentOutputs.Add(output);
                }
            }

            foreach (UtxoOutput output in update.NewOutputs)
            {
                UnspentOutputs.Add(output.OutputPoint, output);
            }
        }
    }
}