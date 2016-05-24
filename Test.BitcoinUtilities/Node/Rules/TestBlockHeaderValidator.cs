using System;
using BitcoinUtilities;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Node.Rules
{
    [TestFixture]
    public class TestBlockHeaderValidator
    {
        private static readonly BlockHeader genesisBlockHeader = GenesisBlock.GetHeader();

        //todo: generate blocks to test BlockHeaderValidator.IsValid method instead of testing each method ?

        [Test]
        public void TestIsHashValid()
        {
            Assert.True(BlockHeaderValidator.IsHashValid(genesisBlockHeader));

            BlockHeader blockHeader = new BlockHeader(
                genesisBlockHeader.Version,
                genesisBlockHeader.PrevBlock,
                genesisBlockHeader.MerkleRoot,
                genesisBlockHeader.Timestamp,
                genesisBlockHeader.NBits,
                1);
            Assert.False(BlockHeaderValidator.IsHashValid(blockHeader));
        }

        [Test]
        public void TestIsTimestampValid()
        {
            BlockHeader blockHeader1 = new BlockHeader(
                genesisBlockHeader.Version,
                genesisBlockHeader.PrevBlock,
                genesisBlockHeader.MerkleRoot,
                (uint) UnixTime.ToTimestamp(DateTime.UtcNow.AddMinutes(110)),
                genesisBlockHeader.NBits,
                genesisBlockHeader.Nonce);
            Assert.True(BlockHeaderValidator.IsTimestampValid(blockHeader1));

            BlockHeader blockHeader2 = new BlockHeader(
                genesisBlockHeader.Version,
                genesisBlockHeader.PrevBlock,
                genesisBlockHeader.MerkleRoot,
                (uint) UnixTime.ToTimestamp(DateTime.UtcNow.AddMinutes(130)),
                genesisBlockHeader.NBits,
                genesisBlockHeader.Nonce);
            Assert.False(BlockHeaderValidator.IsTimestampValid(blockHeader2));
        }
    }
}