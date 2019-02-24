using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Node.Modules.Wallet.Events;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Scripts;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Modules.Wallet
{
    public class Wallet : IUpdatableOutputSet<WalletOutput>
    {
        private readonly object monitor = new object();

        private readonly NetworkParameters networkParameters;
        private readonly IEventDispatcher eventDispatcher;

        private readonly BitcoinNetworkKind networkKind;

        // todo: use public key instead of address
        private readonly Dictionary<string, byte[]> addresses = new Dictionary<string, byte[]>();
        private readonly OutputSet outputs = new OutputSet();

        public Wallet(NetworkParameters networkParameters, IEventDispatcher eventDispatcher)
        {
            this.networkParameters = networkParameters;
            this.eventDispatcher = eventDispatcher;
            // todo: use real implementation
            networkKind = networkParameters.Name.Contains("test") ? BitcoinNetworkKind.Test : BitcoinNetworkKind.Main;
        }

        public byte[] GetPrivateKey(string address)
        {
            if (!addresses.TryGetValue(address, out var privateKey))
            {
                return null;
            }

            return privateKey;
        }

        public void AddPrivateKey(byte[] privateKey)
        {
            lock (monitor)
            {
                addresses[BitcoinAddress.FromPrivateKey(networkKind, privateKey, false)] = privateKey;
                addresses[BitcoinAddress.FromPrivateKey(networkKind, privateKey, true)] = privateKey;

                outputs.Clear();
                eventDispatcher.Raise(new WalletAddressChangedEvent());
                eventDispatcher.Raise(new WalletBalanceChangedEvent());
            }
        }

        public IReadOnlyList<string> GetAddresses()
        {
            lock (monitor)
            {
                return new List<string>(addresses.Keys);
            }
        }

        public IReadOnlyList<WalletOutput> GetAllOutputs()
        {
            lock (monitor)
            {
                return new List<WalletOutput>(outputs);
            }
        }

        public IReadOnlyCollection<WalletOutput> FindUnspentOutputs(byte[] transactionHash)
        {
            lock (monitor)
            {
                return new List<WalletOutput>(outputs.GetByTxHash(transactionHash));
            }
        }

        public WalletOutput FindUnspentOutput(TxOutPoint outPoint)
        {
            lock (monitor)
            {
                if (!outputs.TryGetValue(outPoint, out var output))
                {
                    return null;
                }

                return output;
            }
        }

        public void CreateUnspentOutput(byte[] txHash, int outputIndex, ulong value, byte[] pubkeyScript)
        {
            string address = BitcoinScript.GetAddressFromPubkeyScript(networkKind, pubkeyScript);
            if (address == null)
            {
                return;
            }

            lock (monitor)
            {
                if (addresses.ContainsKey(address))
                {
                    outputs.Add(new WalletOutput(address, txHash, outputIndex, value, pubkeyScript));
                    eventDispatcher.Raise(new WalletBalanceChangedEvent());
                }
            }
        }

        public void Spend(WalletOutput output)
        {
            lock (monitor)
            {
                if (outputs.Remove(output.OutPoint))
                {
                    eventDispatcher.Raise(new WalletBalanceChangedEvent());
                }
            }
        }

        // todo: this class is same as in UTXO UpdatableOutputSet, except for type
        private class OutputSet : IEnumerable<WalletOutput>
        {
            private readonly Dictionary<byte[], Dictionary<int, WalletOutput>> outputsByTxHash = new Dictionary<byte[], Dictionary<int, WalletOutput>>(ByteArrayComparer.Instance);

            public void Clear()
            {
                outputsByTxHash.Clear();
            }

            public void Add(WalletOutput output)
            {
                if (!outputsByTxHash.TryGetValue(output.OutPoint.Hash, out var outputsByIndex))
                {
                    outputsByIndex = new Dictionary<int, WalletOutput>();
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

            public bool TryGetValue(TxOutPoint outPoint, out WalletOutput output)
            {
                if (!outputsByTxHash.TryGetValue(outPoint.Hash, out var outputsByIndex))
                {
                    output = null;
                    return false;
                }

                return outputsByIndex.TryGetValue(outPoint.Index, out output);
            }

            public IEnumerable<WalletOutput> GetByTxHash(byte[] transactionHash)
            {
                if (!outputsByTxHash.TryGetValue(transactionHash, out var outputsByIndex))
                {
                    return Enumerable.Empty<WalletOutput>();
                }

                return outputsByIndex.Values;
            }

            public IEnumerator<WalletOutput> GetEnumerator()
            {
                foreach (Dictionary<int, WalletOutput> outputsByIndex in outputsByTxHash.Values)
                {
                    foreach (WalletOutput output in outputsByIndex.Values)
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