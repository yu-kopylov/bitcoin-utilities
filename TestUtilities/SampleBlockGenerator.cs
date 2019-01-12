using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using BitcoinUtilities;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Scripts;

namespace TestUtilities
{
    /// <summary>
    /// <para>Generator of sample blocks for tests.</para>
    /// <para>Ignores difficulty rules.</para>
    /// </summary>
    public class SampleBlockGenerator
    {
        private readonly SecureRandom secureRandom = SecureRandom.Create();

        /// <summary>
        /// Generated a block that follows a block with the giveh hash. 
        /// </summary>
        /// <param name="parentHash">The hash of the previous block header.</param>
        public BlockMessage Generate(byte[] parentHash)
        {
            var privateKey = BitcoinPrivateKey.Create(secureRandom);
            var address = BitcoinAddress.FromPrivateKey(BitcoinNetworkKind.Test, privateKey, true);
            Tx coinbaseTx = new Tx
            (
                version: 1,
                inputs: new TxIn[] {new TxIn(new TxOutPoint(new byte[32], -1), Encoding.ASCII.GetBytes("TEST"), 0xFFFFFFFF)},
                outputs: new TxOut[] {new TxOut(50_00_000_000, BitcoinScript.CreatePayToPubkeyHash(address))},
                lockTime: 0
            );
            Tx[] transactions = new Tx[] {coinbaseTx};
            BlockHeader header = new BlockHeader
            (
                version: 4,
                prevBlock: parentHash,
                merkleRoot: MerkleTreeUtils.GetTreeRoot(transactions),
                timestamp: (uint) UnixTime.ToTimestamp(DateTime.UtcNow),
                nBits: DifficultyUtils.TargetToNBits(BigInteger.Pow(2, 256)),
                nonce: 0
            );
            return new BlockMessage(header, transactions);
        }

        /// <summary>
        /// Generated a chain of blocks that follow a block with the giveh hash. 
        /// </summary>
        /// <param name="parentHash">The hash of the previous block header.</param>
        /// <param name="count">The number of blocks to genearate.</param>
        public List<BlockMessage> GenerateChain(byte[] parentHash, int count)
        {
            List<BlockMessage> blocks = new List<BlockMessage>(count);
            for (int i = 0; i < count; i++)
            {
                BlockMessage block = Generate(parentHash);
                blocks.Add(block);
                parentHash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(block.BlockHeader.Write));
            }

            return blocks;
        }
    }
}