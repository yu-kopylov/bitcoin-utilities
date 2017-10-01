using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using BitcoinUtilities;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Storage;
using BitcoinUtilities.Storage.SQLite;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities.Storage.SQLite
{
    [TestFixture]
    public class TestSQLiteBlockchainStorage
    {
        [Test]
        public void TestSmoke()
        {
            string testFolder = TestUtils.PrepareTestFolder(GetType(), nameof(TestSmoke), "*.db");

            using (SQLiteBlockchainStorage storage = SQLiteBlockchainStorage.Open(testFolder))
            {
                Assert.Null(storage.FindBlockByHash(GenesisBlock.Hash));
                Assert.Null(storage.FindFirst(new BlockSelector
                {
                    IsInBestHeaderChain = true,
                    IsInBestBlockChain = true,
                    Order = BlockSelector.SortOrder.Height,
                    Direction = BlockSelector.SortDirection.Desc
                }));
                Assert.That(storage.Find(new BlockSelector
                {
                    Heights = new int[] {0, 1},
                    IsInBestHeaderChain = true,
                    Order = BlockSelector.SortOrder.Height,
                    Direction = BlockSelector.SortDirection.Asc
                }, 100).Select(b => b.Height).ToList(), Is.EqualTo(new int[] {}));

                StoredBlockBuilder blockBuilder = new StoredBlockBuilder(GenesisBlock.GetHeader());
                blockBuilder.Height = 0;
                blockBuilder.IsInBestHeaderChain = true;
                blockBuilder.IsInBestBlockChain = true;

                storage.AddBlock(blockBuilder.Build());
            }

            using (SQLiteBlockchainStorage storage = SQLiteBlockchainStorage.Open(testFolder))
            {
                StoredBlock block = storage.FindBlockByHash(GenesisBlock.Hash);

                Assert.NotNull(block);
                Assert.That(block.Hash, Is.EqualTo(GenesisBlock.Hash));

                block = storage.FindFirst(new BlockSelector
                {
                    IsInBestHeaderChain = true,
                    IsInBestBlockChain = true,
                    Order = BlockSelector.SortOrder.Height,
                    Direction = BlockSelector.SortDirection.Desc
                });

                Assert.NotNull(block);
                Assert.That(block.Hash, Is.EqualTo(GenesisBlock.Hash));

                Assert.That(storage.Find(new BlockSelector
                {
                    Heights = new int[] {0, 1},
                    IsInBestHeaderChain = true,
                    Order = BlockSelector.SortOrder.Height,
                    Direction = BlockSelector.SortDirection.Asc
                }, 100).Select(b => b.Height).ToList(), Is.EqualTo(new int[] {0}));
            }
        }

        [Test]
        public void TestFindFirstErrors()
        {
            string testFolder = TestUtils.PrepareTestFolder(GetType(), nameof(TestFindFirstErrors), "*.db");

            using (SQLiteBlockchainStorage storage = SQLiteBlockchainStorage.Open(testFolder))
            {
                Assert.Throws<ArgumentException>(() => storage.FindFirst(new BlockSelector()));
            }
        }

        [Test]
        public void TestFindErrors()
        {
            string testFolder = TestUtils.PrepareTestFolder(GetType(), $"{nameof(TestFindErrors)}", "*.db");

            using (SQLiteBlockchainStorage storage = SQLiteBlockchainStorage.Open(testFolder))
            {
                Assert.Throws<ArgumentException>(() => storage.Find(new BlockSelector(), 1));

                Assert.Throws<ArgumentException>(() => storage.Find(BlockSelector.LastBestHeader, 0));
                Assert.That(storage.Find(BlockSelector.LastBestHeader, 1).Count, Is.EqualTo(0));
                Assert.That(storage.Find(BlockSelector.LastBestHeader, 256).Count, Is.EqualTo(0));
                Assert.Throws<ArgumentException>(() => storage.Find(BlockSelector.LastBestHeader, 257));
            }
        }

        [Test]
        public void TestTransactionsWithCache()
        {
            string testFolder = TestUtils.PrepareTestFolder(GetType(), nameof(TestTransactionsWithCache), "*.db");

            using (SQLiteBlockchainStorage storage = SQLiteBlockchainStorage.Open(testFolder))
            {
                CachingBlockchainStorage cache = new CachingBlockchainStorage(storage);

                StoredBlockBuilder blockBuilder = new StoredBlockBuilder(GenesisBlock.GetHeader());
                blockBuilder.IsInBestHeaderChain = true;
                blockBuilder.IsInBestBlockChain = false;
                StoredBlock block = blockBuilder.Build();

                Assert.Throws<InvalidOperationException>(() => cache.AddBlock(block));

                using (new TransactionScope(TransactionScopeOption.Required))
                {
                    cache.AddBlock(block);
                    Assert.That(cache.FindBlockByHash(block.Hash), Is.Not.Null);
                    Assert.That(cache.FindSubchain(block.Hash, 10), Is.Not.Null);
                }

                Assert.That(cache.FindBlockByHash(block.Hash), Is.Null);
                Assert.That(cache.FindSubchain(block.Hash, 10), Is.Null);

                using (var tx = new TransactionScope(TransactionScopeOption.Required))
                {
                    cache.AddBlock(block);
                    tx.Complete();
                }

                Assert.That(cache.FindBlockByHash(block.Hash), Is.Not.Null);
                Assert.That(cache.FindSubchain(block.Hash, 10), Is.Not.Null);

                blockBuilder = new StoredBlockBuilder(block);
                blockBuilder.IsInBestBlockChain = true;
                block = blockBuilder.Build();

                Assert.Throws<InvalidOperationException>(() => cache.UpdateBlock(block));

                using (new TransactionScope(TransactionScopeOption.Required))
                {
                    cache.UpdateBlock(block);
                    Assert.True(cache.FindBlockByHash(block.Hash).IsInBestBlockChain);
                    Assert.True(cache.FindSubchain(block.Hash, 10).GetBlockByOffset(0).IsInBestBlockChain);
                }

                Assert.False(cache.FindBlockByHash(block.Hash).IsInBestBlockChain);
                Assert.False(cache.FindSubchain(block.Hash, 10).GetBlockByOffset(0).IsInBestBlockChain);

                using (var tx = new TransactionScope(TransactionScopeOption.Required))
                {
                    cache.UpdateBlock(block);
                    tx.Complete();
                }

                Assert.True(cache.FindBlockByHash(block.Hash).IsInBestBlockChain);
                Assert.True(cache.FindSubchain(block.Hash, 10).GetBlockByOffset(0).IsInBestBlockChain);

                Assert.Throws<InvalidOperationException>(() => cache.AddBlockContent(block.Hash, GenesisBlock.Raw));

                using (new TransactionScope(TransactionScopeOption.Required))
                {
                    cache.AddBlockContent(block.Hash, GenesisBlock.Raw);
                    Assert.True(cache.FindBlockByHash(block.Hash).HasContent);
                    Assert.True(cache.FindSubchain(block.Hash, 10).GetBlockByOffset(0).HasContent);
                }

                Assert.False(cache.FindBlockByHash(block.Hash).HasContent);
                Assert.False(cache.FindSubchain(block.Hash, 10).GetBlockByOffset(0).HasContent);

                using (var tx = new TransactionScope(TransactionScopeOption.Required))
                {
                    cache.AddBlockContent(block.Hash, GenesisBlock.Raw);
                    tx.Complete();
                }

                Assert.True(cache.FindBlockByHash(block.Hash).HasContent);
                Assert.True(cache.FindSubchain(block.Hash, 10).GetBlockByOffset(0).HasContent);
            }
        }

        [Test]
        public void TestTransactionsWithInternalBlockchain()
        {
            string testFolder = TestUtils.PrepareTestFolder(GetType(), nameof(TestTransactionsWithInternalBlockchain), "*.db");

            using (SQLiteBlockchainStorage storage = SQLiteBlockchainStorage.Open(testFolder))
            {
                CachingBlockchainStorage cache = new CachingBlockchainStorage(storage);
                InternalBlockchain blockchain = new InternalBlockchain(NetworkParameters.BitcoinCoreMain, cache);

                using (var tx = new TransactionScope(TransactionScopeOption.Required))
                {
                    blockchain.Init();
                    tx.Complete();
                }

                using (new TransactionScope(TransactionScopeOption.Required))
                {
                    Assert.That(blockchain.CommitedState.BestHeader.Height, Is.EqualTo(0));
                    Assert.That(blockchain.CommitedState.BestChain.Height, Is.EqualTo(0));
                    blockchain.AddHeaders(new List<StoredBlock> {new StoredBlockBuilder(BitcoinStreamReader.FromBytes(KnownBlocks.Block1, BlockHeader.Read)).Build()});
                }

                using (var tx = new TransactionScope(TransactionScopeOption.Required))
                {
                    Assert.That(blockchain.CommitedState.BestHeader.Height, Is.EqualTo(0));
                    Assert.That(blockchain.CommitedState.BestChain.Height, Is.EqualTo(0));
                    blockchain.AddHeaders(new List<StoredBlock> {new StoredBlockBuilder(BitcoinStreamReader.FromBytes(KnownBlocks.Block1, BlockHeader.Read)).Build()});
                    tx.Complete();
                }

                using (new TransactionScope(TransactionScopeOption.Required))
                {
                    Assert.That(blockchain.CommitedState.BestHeader.Height, Is.EqualTo(1));
                    Assert.That(blockchain.CommitedState.BestChain.Height, Is.EqualTo(0));
                }
            }
        }

        [Test]
        public void TestUnspentOutputs()
        {
            string testFolder = TestUtils.PrepareTestFolder(GetType(), nameof(TestUnspentOutputs), "*.db");

            using (SQLiteBlockchainStorage storage = SQLiteBlockchainStorage.Open(testFolder))
            {
                byte[] transactionHash = new byte[32];
                transactionHash[0] = 1;

                UnspentOutput output = new UnspentOutput(1, transactionHash, 0, 100, new byte[10]);
                storage.AddUnspentOutput(output);

                List<UnspentOutput> dbOutputs = storage.FindUnspentOutputs(transactionHash);
                Assert.That(dbOutputs.Count, Is.EqualTo(1));

                UnspentOutput dbOutput = dbOutputs[0];
                Assert.That(dbOutput.SourceBlockHeight, Is.EqualTo(output.SourceBlockHeight));
                Assert.That(dbOutput.TransactionHash, Is.EqualTo(output.TransactionHash));
                Assert.That(dbOutput.OutputNumber, Is.EqualTo(output.OutputNumber));
                Assert.That(dbOutput.Sum, Is.EqualTo(output.Sum));
                Assert.That(dbOutput.PublicScript, Is.EqualTo(output.PublicScript));

                storage.RemoveUnspentOutput(transactionHash, 0);
                Assert.That(storage.FindUnspentOutputs(transactionHash), Is.Empty);

                storage.AddSpentOutput(SpentOutput.Create(new StoredBlockBuilder(GenesisBlock.GetHeader()) {Height = 2}.Build(), dbOutput));
            }
        }
    }
}