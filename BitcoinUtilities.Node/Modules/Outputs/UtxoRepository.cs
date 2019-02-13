using System;
using System.Collections.Generic;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Modules.Outputs
{
    public class UtxoRepository : IDisposable
    {
        private readonly UtxoStorage storage;
        private readonly IEventDispatcher eventDispatcher;
        private readonly object monitor = new object();

        public UtxoRepository(UtxoStorage storage, IEventDispatcher eventDispatcher)
        {
            this.storage = storage;
            this.eventDispatcher = eventDispatcher;
        }

        public void Dispose()
        {
            storage.Dispose();
        }

        public UtxoHeader GetLastHeader()
        {
            lock (monitor)
            {
                return storage.GetLastHeader();
            }
        }

        public IReadOnlyCollection<UtxoOutput> GetUnspentOutputs(ISet<byte[]> txHashes)
        {
            lock (monitor)
            {
                return storage.GetUnspentOutputs(txHashes);
            }
        }

        public UtxoHeader Update(IReadOnlyList<UtxoUpdate> updates)
        {
            lock (monitor)
            {
                // todo: what if storage throws exception?
                storage.Update(updates);

                UtxoHeader lastSavedHeader = storage.GetLastHeader();
                eventDispatcher.Raise(new UtxoChangedEvent(lastSavedHeader.Hash, lastSavedHeader.Height));
                return lastSavedHeader;
            }
        }
    }
}