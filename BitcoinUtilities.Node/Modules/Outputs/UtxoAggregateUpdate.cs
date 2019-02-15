using System.Collections.Generic;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Modules.Outputs
{
    /// <summary>
    /// Aggregates operations into two independent sets of spent and created outputs.
    /// Operations that cancel each other do not add outputs to the sets.
    /// </summary>
    public class UtxoAggregateUpdate
    {
        private readonly Dictionary<TxOutPoint, UtxoOutput> createdOutputs = new Dictionary<TxOutPoint, UtxoOutput>();
        private readonly List<TxOutPoint> spentOutputs = new List<TxOutPoint>();

        public IEnumerable<UtxoOutput> CreatedOutputs => createdOutputs.Values;
        public IEnumerable<TxOutPoint> SpentOutputs => spentOutputs;

        public void Clear()
        {
            createdOutputs.Clear();
            spentOutputs.Clear();
        }

        public void Add(IReadOnlyList<UtxoOperation> operations)
        {
            Add(operations, false);
        }

        public void AddReversals(IReadOnlyList<UtxoOperation> operations)
        {
            Add(operations, true);
        }

        private void Add(IReadOnlyList<UtxoOperation> operations, bool revert)
        {
            foreach (var operation in operations)
            {
                if (operation.Spent != revert)
                {
                    if (createdOutputs.Remove(operation.Output.OutPoint))
                    {
                        // Output was created and spent withing the same batch. No changes to UnspentOutputs will be required.
                    }
                    else
                    {
                        spentOutputs.Add(operation.Output.OutPoint);
                    }
                }
                else
                {
                    createdOutputs.Add(operation.Output.OutPoint, operation.Output);
                }
            }
        }
    }
}