using System.Linq;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Node.Modules.Headers;
using BitcoinUtilities.Node.Modules.Wallet.Events;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Modules.Wallet
{
    public class WalletUpdateService : EventHandlingService
    {
        private readonly Blockchain blockchain;
        private readonly Wallet wallet;
        private readonly IEventDispatcher eventDispatcher;

        private DbHeader lastRequestedHeader;
        private DbHeader lastProcessedHeader;
        private HeaderSubChain cachedChainForUpdate;

        public WalletUpdateService(Blockchain blockchain, Wallet wallet, IEventDispatcher eventDispatcher)
        {
            this.blockchain = blockchain;
            this.wallet = wallet;
            this.eventDispatcher = eventDispatcher;
            On<BestHeadChangedEvent>(e => RequestBlocks());
            On<WalletAddressChangedEvent>(e => StartRescan());
            On<BlockAvailableEvent>(ProcessBlock);
        }

        public override void OnStart()
        {
            base.OnStart();
            RequestBlocks();
        }

        private void StartRescan()
        {
            // todo: should wallet outputs should cleared here instead of wallet?
            lastProcessedHeader = null;
            lastRequestedHeader = null;
            RequestBlocks();
        }

        private void RequestBlocks()
        {
            HeaderSubChain chain = GetChainForUpdate(lastProcessedHeader);
            if (chain == null)
            {
                return;
            }

            int requiredBlockHeight = GetNextBlockHeight(lastProcessedHeader);
            DbHeader requiredHeader = chain.GetBlockByHeight(requiredBlockHeight);

            if (lastRequestedHeader == null || !lastRequestedHeader.Hash.SequenceEqual(requiredHeader.Hash))
            {
                lastRequestedHeader = requiredHeader;
                eventDispatcher.Raise(new PrefetchBlocksEvent(this, chain.GetChildSubChain(requiredBlockHeight, 2000)));
                eventDispatcher.Raise(new RequestBlockEvent(this, requiredHeader.Hash));
            }
        }

        private void ProcessBlock(BlockAvailableEvent evt)
        {
            var nextHeader = GetNextHeaderInChain(lastProcessedHeader);
            if (nextHeader != null && nextHeader.Hash.SequenceEqual(evt.HeaderHash))
            {
                ProcessBlock(nextHeader, evt.Block);
                lastProcessedHeader = nextHeader;
            }

            RequestBlocks();
        }

        private void ProcessBlock(DbHeader dbHeader, BlockMessage block)
        {
            // todo: use TransactionProcessor to handle BIP-30 (validation should be removed from TransactionProcessor first) 
            // todo: do not use wallet as IUpdatableOutputSet, create updated for each block instead to allow reversal
            // TransactionProcessor transactionProcessor = new TransactionProcessor();
            // transactionProcessor.UpdateOutputs(wallet, dbHeader.Height, dbHeader.Hash, block);

            foreach (Tx tx in block.Transactions)
            {
                foreach (TxIn input in tx.Inputs)
                {
                    var output = wallet.FindUnspentOutput(input.PreviousOutput);
                    if (output != null)
                    {
                        wallet.Spend(output);
                    }
                }

                for (int idx = 0; idx < tx.Outputs.Length; idx++)
                {
                    var output = tx.Outputs[idx];
                    wallet.CreateUnspentOutput(tx.Hash, idx, output.Value, output.PubkeyScript);
                }
            }
        }

        // todo: GetChainForUpdate, CanUseCachedChainForUpdate and GetNextBlockHeight, GetNextHeaderInChain methods are same as in UtxoUpdateService, except for type.
        private HeaderSubChain GetChainForUpdate(DbHeader utxoHead)
        {
            if (CanUseCachedChainForUpdate(utxoHead))
            {
                return cachedChainForUpdate;
            }

            HeaderSubChain chain = null;

            // best head can become invalid in the process, but there always be another best head
            while (chain == null)
            {
                DbHeader blockChainHead = blockchain.GetBestHead();
                int requiredBlockHeight = GetNextBlockHeight(utxoHead);

                if (requiredBlockHeight > blockChainHead.Height)
                {
                    // todo: allow reversal
                    return null;
                }

                chain = blockchain.GetSubChain(blockChainHead.Hash, blockChainHead.Height - requiredBlockHeight + 1);

                if (chain != null && utxoHead != null)
                {
                    if (!ByteArrayComparer.Instance.Equals(chain.GetBlockByHeight(requiredBlockHeight).ParentHash, utxoHead.Hash))
                    {
                        // UTXO head is on the the wrong branch
                        // todo: allow reversal
                        return null;
                    }
                }
            }

            cachedChainForUpdate = chain;

            return chain;
        }

        private bool CanUseCachedChainForUpdate(DbHeader utxoHead)
        {
            if (cachedChainForUpdate == null)
            {
                return false;
            }

            DbHeader lastChainHead = cachedChainForUpdate.GetBlockByOffset(0);
            DbHeader blockChainHead = blockchain.GetBestHead();

            if (lastChainHead != blockChainHead)
            {
                return false;
            }

            var lastChainTail = cachedChainForUpdate.GetBlockByOffset(cachedChainForUpdate.Count - 1);
            int requiredBlockHeight = GetNextBlockHeight(utxoHead);

            if (requiredBlockHeight < lastChainTail.Height || requiredBlockHeight > lastChainHead.Height)
            {
                return false;
            }

            if (utxoHead == null)
            {
                // no need to check previous hash for the genesis block
                return true;
            }

            DbHeader requiredHeader = cachedChainForUpdate.GetBlockByHeight(requiredBlockHeight);
            return ByteArrayComparer.Instance.Equals(requiredHeader.ParentHash, utxoHead.Hash);
        }

        private DbHeader GetNextHeaderInChain(DbHeader currentHeader)
        {
            HeaderSubChain chain = GetChainForUpdate(currentHeader);

            if (chain == null)
            {
                return null;
            }

            int nextHeaderHeight = GetNextBlockHeight(currentHeader);
            DbHeader nextHeader = chain.GetBlockByHeight(nextHeaderHeight);
            return nextHeader;
        }

        private static int GetNextBlockHeight(DbHeader currentHeader)
        {
            return currentHeader == null ? 0 : currentHeader.Height + 1;
        }
    }
}