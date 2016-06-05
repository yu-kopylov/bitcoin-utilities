using System;
using System.Collections.Generic;
using System.IO;
using BitcoinUtilities;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestMerkleTreeUtils
    {
        [Test]
        public void TestGetTreeRootOnEmptyList()
        {
            Assert.Throws<ArgumentException>(() => MerkleTreeUtils.GetTreeRoot((List<byte[]>) null));
            Assert.Throws<ArgumentException>(() => MerkleTreeUtils.GetTreeRoot(new List<byte[]>()));

            Assert.Throws<ArgumentException>(() => MerkleTreeUtils.GetTreeRoot((Tx[]) null));
            Assert.Throws<ArgumentException>(() => MerkleTreeUtils.GetTreeRoot(new Tx[0]));
        }

        [Test]
        public void TestGetTreeRootOnGenesisBlock()
        {
            BlockMessage block = BlockMessage.Read(new BitcoinStreamReader(new MemoryStream(GenesisBlock.Raw)));
            byte[] calculatedHash = MerkleTreeUtils.GetTreeRoot(block.Transactions);
            Assert.That(calculatedHash, Is.EqualTo(block.BlockHeader.MerkleRoot));
        }

        [Test]
        public void TestGetTreeRootOnBlock100000()
        {
            BlockMessage block = BlockMessage.Read(new BitcoinStreamReader(new MemoryStream(KnownBlocks.Block100000)));
            byte[] calculatedHash = MerkleTreeUtils.GetTreeRoot(block.Transactions);
            Assert.That(calculatedHash, Is.EqualTo(block.BlockHeader.MerkleRoot));
        }

        [Test]
        public void TestGetTreeRootOnBlock100001()
        {
            BlockMessage block = BlockMessage.Read(new BitcoinStreamReader(new MemoryStream(KnownBlocks.Block100001)));
            byte[] calculatedHash = MerkleTreeUtils.GetTreeRoot(block.Transactions);
            Assert.That(calculatedHash, Is.EqualTo(block.BlockHeader.MerkleRoot));
        }

        [Test]
        public void TestGetTreeRootOnBlock100002()
        {
            BlockMessage block = BlockMessage.Read(new BitcoinStreamReader(new MemoryStream(KnownBlocks.Block100002)));
            byte[] calculatedHash = MerkleTreeUtils.GetTreeRoot(block.Transactions);
            Assert.That(calculatedHash, Is.EqualTo(block.BlockHeader.MerkleRoot));
        }
    }
}