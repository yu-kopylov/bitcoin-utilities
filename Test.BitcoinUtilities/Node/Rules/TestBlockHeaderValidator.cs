using System;
using System.Collections.Generic;
using System.Numerics;
using BitcoinUtilities;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Storage;
using NUnit.Framework;
using TestUtilities;

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
            Assert.True(BlockHeaderValidator.IsHashValid(KnownBlocks.Block100000.Header, KnownBlocks.Block100000.Hash));
            Assert.False(BlockHeaderValidator.IsHashValid(KnownBlocks.Block100000.Header, KnownBlocks.Block100000.Header.PrevBlock));

            BlockHeader originalHeader = KnownBlocks.Block100000.Header;
            BigInteger target = NumberUtils.ToBigInteger(KnownBlocks.Block100000.Hash);

            BlockHeader validHeader = new BlockHeader
            (
                originalHeader.Version,
                originalHeader.PrevBlock,
                originalHeader.MerkleRoot,
                originalHeader.Timestamp,
                DifficultyUtils.TargetToNBits(target * 10001 / 10000),
                originalHeader.Timestamp
            );

            BlockHeader invalidHeader = new BlockHeader
            (
                originalHeader.Version,
                originalHeader.PrevBlock,
                originalHeader.MerkleRoot,
                originalHeader.Timestamp,
                DifficultyUtils.TargetToNBits(target * 9999 / 10000),
                originalHeader.Timestamp
            );

            Assert.True(BlockHeaderValidator.IsHashValid(validHeader, KnownBlocks.Block100000.Hash));
            Assert.False(BlockHeaderValidator.IsHashValid(invalidHeader, KnownBlocks.Block100000.Hash));
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