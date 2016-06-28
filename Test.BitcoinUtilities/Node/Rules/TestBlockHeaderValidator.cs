using System;
using System.Collections.Generic;
using BitcoinUtilities;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Storage;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Node.Rules
{
    [TestFixture]
    public class TestBlockHeaderValidator
    {
        private static readonly BlockHeader genesisBlockHeader = GenesisBlock.GetHeader();

        //todo: generate blocks to test BlockHeaderValidator.IsValid method instead of testing each method ?

        [Test]
        public void TestNBitsValidStatic()
        {
            Assert.True(BlockHeaderValidator.IsNBitsValid(genesisBlockHeader));

            Assert.False(BlockHeaderValidator.IsNBitsValid(new BlockHeader(
                genesisBlockHeader.Version,
                genesisBlockHeader.PrevBlock,
                genesisBlockHeader.MerkleRoot,
                genesisBlockHeader.Timestamp,
                0x1D010000,
                genesisBlockHeader.Nonce)));
        }

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
        public void TestIsTimestampValidStatic()
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

        [Test]
        public void TestIsTimestampValid()
        {
            List<StoredBlock> blocks = new List<StoredBlock>();

            StoredBlock prevBlock = null;

            for (int i = 0; i < 11; i++)
            {
                BlockHeader header = new BlockHeader
                    (
                    genesisBlockHeader.Version,
                    prevBlock == null ? new byte[32] : prevBlock.Hash,
                    new byte[32],
                    (uint) i,
                    0x21100000,
                    0);
                StoredBlock storedBlock = new StoredBlockBuilder(header).Build();
                blocks.Add(storedBlock);
                prevBlock = storedBlock;
            }

            Subchain subchain = new Subchain(blocks);

            {
                BlockHeader header = new BlockHeader
                    (
                    genesisBlockHeader.Version,
                    new byte[32],
                    new byte[32],
                    5,
                    0x21100000,
                    0);

                StoredBlock storedBlock = new StoredBlockBuilder(header).Build();

                Assert.False(BlockHeaderValidator.IsTimeStampValid(storedBlock, subchain));
            }

            {
                BlockHeader header = new BlockHeader
                    (
                    genesisBlockHeader.Version,
                    new byte[32],
                    new byte[32],
                    6,
                    0x21100000,
                    0);

                StoredBlock storedBlock = new StoredBlockBuilder(header).Build();

                Assert.True(BlockHeaderValidator.IsTimeStampValid(storedBlock, subchain));
            }
        }
    }
}