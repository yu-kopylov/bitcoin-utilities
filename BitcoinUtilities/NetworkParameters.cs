using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Scripts;

namespace BitcoinUtilities
{
    /// <summary>
    /// Operational parameters for particular Bitcoin Network.
    /// </summary>
    public class NetworkParameters
    {
        private const uint NetworkMagicCoreMain = 0xF9BEB4D9;
        private const uint NetworkMagicCoreTestNet = 0x0B110907;
        private const uint NetworkMagicCoreRegTest = 0xFABFB5DA;

        private const uint NetworkMagicCashMain = 0xE3E1F3E8;
        private const uint NetworkMagicCashTestNet = 0xF4E5F3F4;
        private const uint NetworkMagicCashRegTest = 0xDAB5BFFA;

        private static readonly byte[] genesisBlockMain = HexUtils.GetBytesUnsafe
        (
            "0100000000000000000000000000000000000000000000000000000000000000000000003BA3EDFD7A7B12B27AC72C3E67768F617FC81BC3888A51323A9FB8AA" +
            "4B1E5E4A29AB5F49FFFF001D1DAC2B7C0101000000010000000000000000000000000000000000000000000000000000000000000000FFFFFFFF4D04FFFF001D" +
            "0104455468652054696D65732030332F4A616E2F32303039204368616E63656C6C6F72206F6E206272696E6B206F66207365636F6E64206261696C6F75742066" +
            "6F722062616E6B73FFFFFFFF0100F2052A01000000434104678AFDB0FE5548271967F1A67130B7105CD6A828E03909A67962E0EA1F61DEB649F6BC3F4CEF38C4" +
            "F35504E51EC112DE5C384DF7BA0B8D578A4C702B6BF11D5FAC00000000"
        );

        private static readonly byte[] genesisBlockRegTest = HexUtils.GetBytesUnsafe
        (
            "0100000000000000000000000000000000000000000000000000000000000000000000003BA3EDFD7A7B12B27AC72C3E67768F617FC81BC3888A51323A9FB8AA" +
            "4B1E5E4ADAE5494DFFFF7F20020000000101000000010000000000000000000000000000000000000000000000000000000000000000FFFFFFFF4D04FFFF001D" +
            "0104455468652054696D65732030332F4A616E2F32303039204368616E63656C6C6F72206F6E206272696E6B206F66207365636F6E64206261696C6F75742066" +
            "6F722062616E6B73FFFFFFFF0100F2052A01000000434104678AFDB0FE5548271967F1A67130B7105CD6A828E03909A67962E0EA1F61DEB649F6BC3F4CEF38C4" +
            "F35504E51EC112DE5C384DF7BA0B8D578A4C702B6BF11D5FAC00000000"
        );

        /// <summary>
        /// Target that matches minimum difficulty of the main network.
        /// </summary>
        private static readonly BigInteger MaxDifficultyTargetMain = DifficultyUtils.NBitsToTarget(0x1D00FFFF);

        /// <summary>
        /// Target that matches minimum difficulty of the regtest network.
        /// </summary>
        private static readonly BigInteger MaxDifficultyTargetRegTest = DifficultyUtils.NBitsToTarget(0x207FFFFF);

        private static readonly NetworkParameters[] knownNetworks = new[]
        {
            BitcoinCoreMain,
            BitcoinCoreRegTest,
            BitcoinCashMain,
            BitcoinCashRegTest
        };

        private readonly List<string> dnsSeeds = new List<string>();

        private readonly Dictionary<int, byte[]> checkpoints = new Dictionary<int, byte[]>();

        /// <summary>
        /// Creates a new parameters object.
        /// </summary>
        /// <param name="fork">The fork that is used in the network.</param>
        /// <param name="name">
        /// <para>The name of the network.</para>
        /// <para>It must be a valid file name, because it is used as part of the path to blockchain data.</para>
        /// </param>
        /// <param name="networkMagic">Four defined bytes which start every message in the Bitcoin P2P protocol to allow seeking to the next message.</param>
        /// <param name="maxDifficultyTarget">Maximum difficulty target for the network. It defines minimum difficulty of the network.</param>
        /// <param name="genesisBlock">The first block of the blockchain.</param>
        /// <param name="networkKind">Kind of the network.</param>
        /// <param name="addressConverter">Address converter that defines address format.</param>
        public NetworkParameters(
            BitcoinFork fork,
            string name,
            uint networkMagic,
            BigInteger maxDifficultyTarget,
            byte[] genesisBlock,
            BitcoinNetworkKind networkKind,
            IAddressConverter addressConverter,
            ISigHashCalculatorFactory sigHashCalculatorFactory
        )
        {
            Fork = fork;
            Name = name;
            NetworkMagic = networkMagic;
            MaxDifficultyTarget = maxDifficultyTarget;
            GenesisBlock = BitcoinStreamReader.FromBytes(genesisBlock, BlockMessage.Read);
            NetworkKind = networkKind;
            AddressConverter = addressConverter;
            SigHashCalculatorFactory = sigHashCalculatorFactory;
        }

        public static IReadOnlyList<NetworkParameters> KnownNetworks => knownNetworks;

        public static bool TryGetByName(string name, out NetworkParameters networkParameters)
        {
            networkParameters = knownNetworks.FirstOrDefault(n => n.Name == name);
            return networkParameters != null;
        }

        /// <summary>
        /// The fork that is used in the network.
        /// </summary>
        public BitcoinFork Fork { get; }

        /// <summary>
        /// <para>The name of the network.</para>
        /// <para> It must be a valid file name, because it is used as part of the path to blockchain data.</para>
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Four defined bytes which start every message in the Bitcoin P2P protocol to allow seeking to the next message.
        /// </summary>
        /// <remarks>
        /// See: https://bitcoin.org/en/glossary/start-string
        /// </remarks>
        public uint NetworkMagic { get; }

        /// <summary>
        /// Maximum difficulty target for the network. It defines minimum difficulty of the network.
        /// </summary>
        /// <remarks>
        /// See: https://bitcoin.org/en/developer-reference#target-nbits
        /// </remarks>
        public BigInteger MaxDifficultyTarget { get; }

        /// <summary>
        /// Flag indicating whether difficulty target should be calculated based on previous blocks or remain constant.
        /// </summary>
        public bool DifficultyAdjustmentEnabled { get; private set; } = true;

        /// <summary>
        /// The first block of the blockchain.
        /// </summary>
        public BlockMessage GenesisBlock { get; }

        /// <summary>
        /// Kind of the network.
        /// </summary>
        public BitcoinNetworkKind NetworkKind { get; }

        /// <summary>
        /// Address converter that defines address format.
        /// </summary>
        public IAddressConverter AddressConverter { get; }

        /// <summary>
        /// A factory that can create a calculator that can calculate a digest for a spending transaction. 
        /// </summary>
        public ISigHashCalculatorFactory SigHashCalculatorFactory { get; }

        [SuppressMessage("ReSharper", "CommentTypo")]
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public static NetworkParameters BitcoinCoreMain
        {
            get
            {
                var parameters = new NetworkParameters
                (
                    BitcoinFork.Core,
                    "bitcoin-core-main",
                    NetworkMagicCoreMain,
                    MaxDifficultyTargetMain,
                    genesisBlockMain,
                    BitcoinNetworkKind.Main,
                    new BitcoinCoreAddressConverter(BitcoinNetworkKind.Main),
                    new BitcoinCoreSigHashCalculatorFactory()
                );

                // DNS seeds taken from: https://github.com/bitcoin/bitcoin/blob/master/src/chainparams.cpp
                parameters.AddDnsSeed("seed.bitcoin.sipa.be"); // Pieter Wuille
                parameters.AddDnsSeed("dnsseed.bluematt.me"); // Matt Corallo
                parameters.AddDnsSeed("dnsseed.bitcoin.dashjr.org"); // Luke Dashjr
                parameters.AddDnsSeed("seed.bitcoinstats.com"); // Christian Decker
                parameters.AddDnsSeed("seed.bitcoin.jonasschnelli.ch"); // Jonas Schnelli
                parameters.AddDnsSeed("seed.btc.petertodd.org"); // Peter Todd

                parameters.AddCheckpoint(000001, "00000000839a8e6886ab5951d76f411475428afc90947ee320161bbf18eb6048");
                parameters.AddCheckpoint(000010, "000000002c05cc2e78923c34df87fd108b22221ac6076c18f3ade378a4d915e9");
                parameters.AddCheckpoint(000100, "000000007bc154e0fa7ea32218a72fe2c1bb9f86cf8c9ebf9a715ed27fdb229a");
                parameters.AddCheckpoint(001000, "00000000c937983704a73af28acdec37b049d214adbda81d7e2a3dd146f6ed09");
                parameters.AddCheckpoint(010000, "0000000099c744455f58e6c6e98b671e1bf7f37346bfd4cf5d0274ad8ee660cb");
                parameters.AddCheckpoint(100000, "000000000003ba27aa200b1cecaad478d2b00432346c3f1f3986da1afd33e506");
                parameters.AddCheckpoint(200000, "000000000000034a7dedef4a161fa058a2d67a173a90155f3a2fe6fc132e0ebf");
                parameters.AddCheckpoint(300000, "000000000000000082ccf8f1557c5d40b21edabb18d2d691cfbf87118bac7254");
                parameters.AddCheckpoint(400000, "000000000000000004ec466ce4732fe6f1ed1cddc2ed4b328fff5224276e3f6f");
                parameters.AddCheckpoint(478558, "0000000000000000011865af4122fe3b144e2cbeea86142e8ff2fb4107352d43");
                parameters.AddCheckpoint(478559, "00000000000000000019f112ec0a9982926f1258cdcc558dd7c3b7e5dc7fa148");

                return parameters;
            }
        }

        public static NetworkParameters BitcoinCoreRegTest
        {
            get
            {
                var parameters = new NetworkParameters
                (
                    BitcoinFork.Core,
                    "bitcoin-core-regtest",
                    NetworkMagicCoreRegTest,
                    MaxDifficultyTargetRegTest,
                    genesisBlockRegTest,
                    BitcoinNetworkKind.Test,
                    new BitcoinCoreAddressConverter(BitcoinNetworkKind.Test),
                    new BitcoinCoreSigHashCalculatorFactory()
                );
                parameters.DifficultyAdjustmentEnabled = false;
                return parameters;
            }
        }

        [SuppressMessage("ReSharper", "CommentTypo")]
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public static NetworkParameters BitcoinCashMain
        {
            get
            {
                var parameters = new NetworkParameters
                (
                    BitcoinFork.Cash,
                    "bitcoin-cash-main",
                    NetworkMagicCashMain,
                    MaxDifficultyTargetMain,
                    genesisBlockMain,
                    BitcoinNetworkKind.Main,
                    new CashAddrConverter("bitcoincash"),
                    new BitcoinCashSigHashCalculatorFactory()
                );

                // DNS seeds taken from: https://github.com/bitcoinclassic/bitcoinclassic/blob/master/src/chainparams.cpp
                parameters.AddDnsSeed("cash-seed.bitcoin.thomaszander.se");
                parameters.AddDnsSeed("seed.bitprim.org");

                // DNS seeds taken from: https://github.com/Bitcoin-ABC/bitcoin-abc/blob/master/src/chainparams.cpp
                // Bitcoin ABC seeder
                parameters.AddDnsSeed("seed.bitcoinabc.org");
                // bitcoinforks seeders
                parameters.AddDnsSeed("seed-abc.bitcoinforks.org");
                // BU backed seeder
                parameters.AddDnsSeed("btccash-seeder.bitcoinunlimited.info");
                // Bitprim
                parameters.AddDnsSeed("seed.bitprim.org");
                // Amaury SÉCHET
                parameters.AddDnsSeed("seed.deadalnix.me");
                // criptolayer.net
                parameters.AddDnsSeed("seeder.criptolayer.net");

                parameters.AddCheckpoint(000001, "00000000839a8e6886ab5951d76f411475428afc90947ee320161bbf18eb6048");
                parameters.AddCheckpoint(000010, "000000002c05cc2e78923c34df87fd108b22221ac6076c18f3ade378a4d915e9");
                parameters.AddCheckpoint(000100, "000000007bc154e0fa7ea32218a72fe2c1bb9f86cf8c9ebf9a715ed27fdb229a");
                parameters.AddCheckpoint(001000, "00000000c937983704a73af28acdec37b049d214adbda81d7e2a3dd146f6ed09");
                parameters.AddCheckpoint(010000, "0000000099c744455f58e6c6e98b671e1bf7f37346bfd4cf5d0274ad8ee660cb");
                parameters.AddCheckpoint(100000, "000000000003ba27aa200b1cecaad478d2b00432346c3f1f3986da1afd33e506");
                parameters.AddCheckpoint(200000, "000000000000034a7dedef4a161fa058a2d67a173a90155f3a2fe6fc132e0ebf");
                parameters.AddCheckpoint(300000, "000000000000000082ccf8f1557c5d40b21edabb18d2d691cfbf87118bac7254");
                parameters.AddCheckpoint(400000, "000000000000000004ec466ce4732fe6f1ed1cddc2ed4b328fff5224276e3f6f");
                parameters.AddCheckpoint(478558, "0000000000000000011865af4122fe3b144e2cbeea86142e8ff2fb4107352d43");
                parameters.AddCheckpoint(478559, "000000000000000000651ef99cb9fcbe0dadde1d424bd9f15ff20136191a5eec");
                parameters.AddCheckpoint(500000, "000000000000000005e14d3f9fdfb70745308706615cfa9edca4f4558332b201");
                parameters.AddCheckpoint(504032, "00000000000000000343e9875012f2062554c8752929892c82a0c0743ac7dcfd");

                return parameters;
            }
        }

        public static NetworkParameters BitcoinCashRegTest
        {
            get
            {
                var parameters = new NetworkParameters
                (
                    BitcoinFork.Cash,
                    "bitcoin-cash-regtest",
                    NetworkMagicCashRegTest,
                    MaxDifficultyTargetRegTest,
                    genesisBlockRegTest,
                    BitcoinNetworkKind.Test,
                    new CashAddrConverter("bchreg"),
                    new BitcoinCashSigHashCalculatorFactory()
                );
                parameters.DifficultyAdjustmentEnabled = false;
                return parameters;
            }
        }

        /// <summary>
        /// Adds a DNS seed if it was not added before.
        /// </summary>
        /// <param name="dnsSeed">The DNS seed.</param>
        private void AddDnsSeed(string dnsSeed)
        {
            if (dnsSeeds.Contains(dnsSeed))
            {
                return;
            }

            dnsSeeds.Add(dnsSeed);
        }

        /// <summary>
        /// Returns an array of DNS seeds.
        /// </summary>
        /// <returns></returns>
        public string[] GetDnsSeeds()
        {
            return dnsSeeds.ToArray();
        }

        /// <summary>
        /// Adds a known checkpoint in blockchain.
        /// </summary>
        /// <param name="blockHeight">The height of the block.</param>
        /// <param name="blockHash">Hash of the block.</param>
        private void AddCheckpoint(int blockHeight, string blockHash)
        {
            if (!HexUtils.TryGetBytes(blockHash, out var hash))
            {
                throw new ArgumentException($"The given {nameof(blockHash)} '{blockHash}' is not a valid hexadecimal string.", nameof(blockHash));
            }

            Array.Reverse(hash);
            checkpoints.Add(blockHeight, hash);
        }

        /// <summary>
        /// Returns a known block hash for the specified block height.
        /// </summary>
        /// <param name="blockHeight">The height of the block.</param>
        /// <returns>The hash for the block with the given height; or null if there is no checkpoint defined for this height.</returns>
        public byte[] GetCheckpointHash(int blockHeight)
        {
            if (checkpoints.TryGetValue(blockHeight, out var hash))
            {
                return hash;
            }

            return null;
        }
    }
}