using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Modules.Outputs
{
    public class UpdatableOutputSet : IUpdatableOutputSet<UtxoOutput>
    {
        private readonly HashSet<byte[]> existingTransactions = new HashSet<byte[]>(ByteArrayComparer.Instance);
        private readonly OutputSet outputs = new OutputSet();
        private readonly List<UtxoOperation> operations = new List<UtxoOperation>();

        public bool HasExistingTransaction(byte[] txHash)
        {
            return existingTransactions.Contains(txHash);
        }

        public IReadOnlyList<UtxoOperation> Operations => operations;

        public void AppendExistingUnspentOutputs(IEnumerable<UtxoOutput> existingOutputs)
        {
            HashSet<byte[]> appendedTransactions = new HashSet<byte[]>(ByteArrayComparer.Instance);
            foreach (UtxoOutput output in existingOutputs)
            {
                if (existingTransactions.Contains(output.OutPoint.Hash))
                {
                    throw new InvalidOperationException(
                        $"An output from the transaction '{HexUtils.GetString(output.OutPoint.Hash)}'" +
                        $" was already added to the {nameof(UpdatableOutputSet)}." +
                        $" All outputs from one transaction should be added at the same time for BIP-30 validation to work." +
                        $" Use '{nameof(HasExistingTransaction)}' method to avoid fetching already included transactions from a database."
                    );
                }

                outputs.Add(output);
                appendedTransactions.Add(output.OutPoint.Hash);
            }

            existingTransactions.UnionWith(appendedTransactions);
        }

        public void ClearOperations()
        {
            operations.Clear();
        }

        public IReadOnlyCollection<UtxoOutput> FindUnspentOutputs(byte[] transactionHash)
        {
            List<UtxoOutput> res = new List<UtxoOutput>();
            res.AddRange(outputs.GetByTxHash(transactionHash));
            return res;
        }

        public UtxoOutput FindUnspentOutput(TxOutPoint outPoint)
        {
            if (outputs.TryGetValue(outPoint, out var output))
            {
                return output;
            }

            return null;
        }

        public void CreateUnspentOutput(byte[] txHash, int outputIndex, ulong value, byte[] pubkeyScript)
        {
            var output = new UtxoOutput(new TxOutPoint(txHash, outputIndex), value, pubkeyScript);
            outputs.Add(output);
            operations.Add(UtxoOperation.Create(output));
        }

        public void Spend(UtxoOutput output)
        {
            if (!outputs.Remove(output.OutPoint))
            {
                throw new InvalidOperationException($"Attempt to spend a non-existent output '{output.OutPoint}'.");
            }

            operations.Add(UtxoOperation.Spend(output));
        }

        private class OutputSet : IEnumerable<UtxoOutput>
        {
            private readonly Dictionary<byte[], Dictionary<int, UtxoOutput>> outputsByTxHash = new Dictionary<byte[], Dictionary<int, UtxoOutput>>(ByteArrayComparer.Instance);

            public void Add(UtxoOutput output)
            {
                if (!outputsByTxHash.TryGetValue(output.OutPoint.Hash, out var outputsByIndex))
                {
                    outputsByIndex = new Dictionary<int, UtxoOutput>();
                    outputsByTxHash.Add(output.OutPoint.Hash, outputsByIndex);
                }

                outputsByIndex.Add(output.OutPoint.Index, output);
            }

            public bool Remove(TxOutPoint outPoint)
            {
                if (!outputsByTxHash.TryGetValue(outPoint.Hash, out var outputsByIndex))
                {
                    return false;
                }

                if (!outputsByIndex.Remove(outPoint.Index))
                {
                    return false;
                }

                if (outputsByIndex.Count == 0)
                {
                    outputsByTxHash.Remove(outPoint.Hash);
                }

                return true;
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