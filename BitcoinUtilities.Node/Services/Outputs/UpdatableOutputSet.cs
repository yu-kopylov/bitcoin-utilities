using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Services.Outputs
{
    public class UpdatableOutputSet : IUpdatableOutputSet<UtxoOutput>
    {
        private readonly HashSet<byte[]> existingTransactions = new HashSet<byte[]>(ByteArrayComparer.Instance);
        private readonly OutputSet existingUnspentOutputs = new OutputSet();
        private readonly List<UtxoOutput> existingSpentOutputs = new List<UtxoOutput>();

        private readonly OutputSet createdUnspentOutputs = new OutputSet();
        private readonly List<UtxoOutput> createdSpentOutputs = new List<UtxoOutput>();

        public bool HasExistingTransaction(byte[] txHash)
        {
            return existingTransactions.Contains(txHash);
        }

        public IEnumerable<UtxoOutput> ExistingSpentOutputs => existingSpentOutputs;
        public IEnumerable<UtxoOutput> CreatedUnspentOutputs => createdUnspentOutputs;
        public IEnumerable<UtxoOutput> CreatedSpentOutputs => createdSpentOutputs;

        public void AppendExistingUnspentOutputs(IEnumerable<UtxoOutput> outputs)
        {
            HashSet<byte[]> appendedTransactions = new HashSet<byte[]>(ByteArrayComparer.Instance);
            foreach (UtxoOutput output in outputs)
            {
                if (existingTransactions.Contains(output.OutputPoint.Hash))
                {
                    throw new InvalidOperationException(
                        $"An output from the transaction '{HexUtils.GetString(output.OutputPoint.Hash)}'" +
                        $" was already added to the {nameof(UpdatableOutputSet)}." +
                        $" All outputs from one transaction should be added at the same time for BIP-30 validation to work." +
                        $" Use '{nameof(HasExistingTransaction)}' method to avoid fetching already included transactions from a database."
                    );
                }

                existingUnspentOutputs.Add(output);
                appendedTransactions.Add(output.OutputPoint.Hash);
            }

            existingTransactions.UnionWith(appendedTransactions);
        }

        public IReadOnlyCollection<UtxoOutput> FindUnspentOutputs(byte[] transactionHash)
        {
            List<UtxoOutput> res = new List<UtxoOutput>();
            res.AddRange(existingUnspentOutputs.GetByTxHash(transactionHash));
            res.AddRange(createdUnspentOutputs.GetByTxHash(transactionHash));
            return res;
        }

        public UtxoOutput FindUnspentOutput(TxOutPoint outPoint)
        {
            if (existingUnspentOutputs.TryGetValue(outPoint, out var output))
            {
                return output;
            }

            if (createdUnspentOutputs.TryGetValue(outPoint, out output))
            {
                return output;
            }

            return null;
        }

        public void CreateUnspentOutput(byte[] txHash, int outputIndex, int height, TxOut txOut)
        {
            createdUnspentOutputs.Add(new UtxoOutput(txHash, outputIndex, height, txOut));
        }

        public void Spend(UtxoOutput output, int blockHeight)
        {
            UtxoOutput spentOutput = output.Spend(blockHeight);
            if (existingUnspentOutputs.Remove(output))
            {
                existingSpentOutputs.Add(spentOutput);
            }
            else if (createdUnspentOutputs.Remove(output))
            {
                createdSpentOutputs.Add(spentOutput);
            }
            else
            {
                throw new InvalidOperationException($"Attempt to spend a non-existent output '{output.OutputPoint}'.");
            }
        }

        private class OutputSet : IEnumerable<UtxoOutput>
        {
            private readonly Dictionary<byte[], Dictionary<int, UtxoOutput>> outputsByTxHash
                = new Dictionary<byte[], Dictionary<int, UtxoOutput>>(ByteArrayComparer.Instance);

            public void Add(UtxoOutput output)
            {
                if (!outputsByTxHash.TryGetValue(output.OutputPoint.Hash, out var outputsByIndex))
                {
                    outputsByIndex = new Dictionary<int, UtxoOutput>();
                    outputsByTxHash.Add(output.OutputPoint.Hash, outputsByIndex);
                }

                outputsByIndex.Add(output.OutputPoint.Index, output);
            }

            public bool Remove(UtxoOutput output)
            {
                if (!outputsByTxHash.TryGetValue(output.OutputPoint.Hash, out var outputsByIndex))
                {
                    return false;
                }

                if (outputsByIndex.TryGetValue(output.OutputPoint.Index, out var existingOutput))
                {
                    if (output != existingOutput)
                    {
                        throw new InvalidOperationException($"Mismatch in output version upon deletion of '{output.OutputPoint}'.");
                    }

                    outputsByIndex.Remove(output.OutputPoint.Index);

                    if (outputsByIndex.Count == 0)
                    {
                        outputsByTxHash.Remove(output.OutputPoint.Hash);
                    }

                    return true;
                }

                return false;
            }

            public bool TryGetValue(TxOutPoint outPoint, out UtxoOutput output)
            {
                if (!outputsByTxHash.TryGetValue(outPoint.Hash, out var outputsByIndex))
                {
                    output = null;
                    return false;
                }

                return outputsByIndex.TryGetValue(outPoint.Index, out output);
            }

            public IEnumerable<UtxoOutput> GetByTxHash(byte[] transactionHash)
            {
                if (!outputsByTxHash.TryGetValue(transactionHash, out var outputsByIndex))
                {
                    return Enumerable.Empty<UtxoOutput>();
                }

                return outputsByIndex.Values;
            }

            public IEnumerator<UtxoOutput> GetEnumerator()
            {
                foreach (Dictionary<int, UtxoOutput> outputsByIndex in outputsByTxHash.Values)
                {
                    foreach (UtxoOutput output in outputsByIndex.Values)
                    {
                        yield return output;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}