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

        private readonly IEventDispatcher eventDispatcher;

        // todo: use public key instead of address
        private readonly List<WalletAddress> addresses = new List<WalletAddress>();
        private readonly Dictionary<byte[], WalletAddress> addressesByPublicKeyHash = new Dictionary<byte[], WalletAddress>(ByteArrayComparer.Instance);
        private readonly Dictionary<byte[], byte[]> privateKeys = new Dictionary<byte[], byte[]>(ByteArrayComparer.Instance);
        private readonly OutputSet outputs = new OutputSet();

        public Wallet(NetworkParameters networkParameters, IEventDispatcher eventDispatcher)
        {
            NetworkParameters = networkParameters;
            this.eventDispatcher = eventDispatcher;
        }

        public NetworkParameters NetworkParameters { get; }

        public byte[] GetPrivateKey(byte[] publicKeyHash)
        {
            if (!privateKeys.TryGetValue(publicKeyHash, out var privateKey))
            {
                return null;
            }

            return privateKey;
        }

        public void AddPrivateKey(byte[] privateKey)
        {
            lock (monitor)
            {
                //todo: move address hashing to utils
                byte[] primaryPublicKeyHash = CryptoUtils.RipeMd160(CryptoUtils.Sha256(BitcoinPrivateKey.ToEncodedPublicKey(privateKey, true)));
                byte[] secondaryPublicKeyHash = CryptoUtils.RipeMd160(CryptoUtils.Sha256(BitcoinPrivateKey.ToEncodedPublicKey(privateKey, false)));

                if (addressesByPublicKeyHash.TryGetValue(primaryPublicKeyHash, out var address1))
                {
                    addresses.Remove(address1);
                }

                if (addressesByPublicKeyHash.TryGetValue(secondaryPublicKeyHash, out var address2))
                {
                    addresses.Remove(address2);
                }

                WalletAddress address = new WalletAddress(primaryPublicKeyHash, secondaryPublicKeyHash);
                addresses.Add(address);

                addressesByPublicKeyHash[primaryPublicKeyHash] = address;
                addressesByPublicKeyHash[secondaryPublicKeyHash] = address;

                privateKeys[primaryPublicKeyHash] = privateKey;
                privateKeys[secondaryPublicKeyHash] = privateKey;

                outputs.Clear();
                foreach (var walletAddress in addresses)
                {
                    walletAddress.Balance = 0;
                }

                eventDispatcher.Raise(new WalletAddressChangedEvent());
                eventDispatcher.Raise(new WalletBalanceChangedEvent());
            }
        }

        public IReadOnlyList<WalletAddress> GetAddresses()
        {
            lock (monitor)
            {
                return new List<WalletAddress>(addresses);
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
            byte[] publicKeyHash = BitcoinScript.GetPublicKeyHashFromPubkeyScript(pubkeyScript);
            if (publicKeyHash == null)
            {
                return;
            }

            lock (monitor)
            {
                if (addressesByPublicKeyHash.TryGetValue(publicKeyHash, out var address))
                {
                    outputs.Add(new WalletOutput(address, publicKeyHash, txHash, outputIndex, value, pubkeyScript));
                    // todo: check for overflow
                    address.Balance += value;
                    eventDispatcher.Raise(new WalletBalanceChangedEvent());
                }
            }
        }

        public void Spend(WalletOutput output)
        {
            lock (monitor)
            {
                if (outputs.TryGetValue(output.OutPoint, out output))
                {
                    output.WalletAddress.Balance -= output.Value;
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

    public class WalletAddress
    {
        // todo: use just one hash instead?
        public WalletAddress(byte[] primaryPublicKeyHash, byte[] secondaryPublicKeyHash)
        {
            PrimaryPublicKeyHash = primaryPublicKeyHash;
            SecondaryPublicKeyHash = secondaryPublicKeyHash;
        }

        public byte[] PrimaryPublicKeyHash { get; }
        public byte[] SecondaryPublicKeyHash { get; }

        public ulong Balance { get; set; }
    }
}