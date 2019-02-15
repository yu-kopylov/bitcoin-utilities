using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Modules.Outputs
{
    public class UtxoRepository : IDisposable
    {
        private const int MaxUnsavedUpdates = 10;

        private readonly UtxoStorage storage;
        private readonly IEventDispatcher eventDispatcher;
        private readonly object monitor = new object();

        private UtxoHeader lastHeader;
        private readonly List<UtxoUpdate> unsavedUpdates = new List<UtxoUpdate>();
        private readonly UtxoAggregateUpdate unsavedOperations = new UtxoAggregateUpdate();

        public UtxoRepository(UtxoStorage storage, IEventDispatcher eventDispatcher)
        {
            this.storage = storage;
            this.eventDispatcher = eventDispatcher;

            lastHeader = storage.GetLastHeader();
        }

        public void Dispose()
        {
            storage.Dispose();
        }

        public UtxoHeader GetLastHeader()
        {
            lock (monitor)
            {
                return lastHeader;
            }
        }

        public IReadOnlyCollection<UtxoOutput> GetUnspentOutputs(ISet<byte[]> txHashes)
        {
            lock (monitor)
            {
                List<UtxoOutput> outputs = new List<UtxoOutput>(storage.GetUnspentOutputs(txHashes));
                HashSet<TxOutPoint> spentOutputs = new HashSet<TxOutPoint>(unsavedOperations.SpentOutputs);

                outputs.RemoveAll(op => spentOutputs.Contains(op.OutPoint));
                outputs.AddRange(unsavedOperations.CreatedOutputs.Where(o => txHashes.Contains(o.OutPoint.Hash)));

                return outputs;
            }
        }

        public void Update(UtxoUpdate update)
        {
            lock (monitor)
            {
                update.CheckFollows(lastHeader);

                unsavedUpdates.Add(update);
                unsavedOperations.Add(update.Operations);

                lastHeader = new UtxoHeader(update.HeaderHash, update.Height, true);
                eventDispatcher.Raise(new UtxoChangedEvent(lastHeader.Hash, lastHeader.Height));

                if (unsavedUpdates.Count > MaxUnsavedUpdates)
                {
                    Flush();
                }
            }
        }

        private void Flush()
        {
            // todo: what if storage throws exception?
            storage.Update(unsavedUpdates);
            unsavedUpdates.Clear();
            unsavedOperations.Clear();
        }
    }
}