using System;
using System.Collections.Generic;

namespace BitcoinUtilities
{
    /// <summary>
    /// Operational parameters for particular Bitcoin Network.
    /// </summary>
    public class NetworkParameters
    {
        private readonly string name;

        private readonly Dictionary<int, byte[]> checkpoints = new Dictionary<int, byte[]>();

        /// <summary>
        /// Creates a new parameters object.
        /// </summary>
        /// <param name="name">
        /// The name of the network.
        /// <para/>
        /// It must be a valid file name, because it is used as part of the path to blockchain data.
        /// </param>
        public NetworkParameters(string name)
        {
            this.name = name;
        }

        public static NetworkParameters BitcoinCoreMain
        {
            get
            {
                NetworkParameters parameters = new NetworkParameters("bitcoin-core-main");
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

        public static NetworkParameters BitcoinCashMain
        {
            get
            {
                NetworkParameters parameters = new NetworkParameters("bitcoin-cash-main");
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
                return parameters;
            }
        }

        /// <summary>
        /// The name of the network.
        /// <para/>
        /// It must be a valid file name, because it is used as part of the path to blockchain data.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Adds a known checkpoint in blockchain.
        /// </summary>
        /// <param name="blockHeight">The height of the block.</param>
        /// <param name="blockHash">Hash of the block.</param>
        public void AddCheckpoint(int blockHeight, string blockHash)
        {
            byte[] hash;
            if (!HexUtils.TryGetBytes(blockHash, out hash))
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
            byte[] hash;
            if (checkpoints.TryGetValue(blockHeight, out hash))
            {
                return hash;
            }
            return null;
        }
    }
}