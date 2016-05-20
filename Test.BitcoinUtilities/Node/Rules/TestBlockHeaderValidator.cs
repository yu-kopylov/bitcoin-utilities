using System.IO;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Node.Rules
{
    [TestFixture]
    public class TestBlockHeaderValidator
    {
        private static readonly BlockHeader genesisBlockHeader = GenesisBlock.GetHeader();

        [Test]
        public void TestValid()
        {
            Assert.That(BlockHeaderValidator.IsValid(genesisBlockHeader));
        }

        [Test]
        public void TestNBitsVsHash()
        {
            BlockHeader blockHeader = new BlockHeader(
                genesisBlockHeader.Version,
                genesisBlockHeader.PrevBlock,
                genesisBlockHeader.MerkleRoot,
                genesisBlockHeader.Timestamp,
                genesisBlockHeader.NBits,
                1);
            Assert.False(BlockHeaderValidator.IsValid(blockHeader));
        }
    }
}