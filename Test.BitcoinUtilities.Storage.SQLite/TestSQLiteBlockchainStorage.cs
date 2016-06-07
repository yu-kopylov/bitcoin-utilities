using System;
using System.Transactions;
using BitcoinUtilities.P2P;
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
            string testFolder = TestUtils.PrepareTestFolder(typeof(TestSQLiteBlockchainStorage), $"{nameof(TestSmoke)}", "*.db");

            using (SQLiteBlockchainStorage storage = SQLiteBlockchainStorage.Open(testFolder))
            {
                Assert.Null(storage.FindBlockByHash(GenesisBlock.Hash));
                Assert.Null(storage.FindBestHeaderChain());

                StoredBlock block = new StoredBlock(GenesisBlock.GetHeader());
                block.IsInBestHeaderChain = true;
                block.IsInBestBlockChain = true;

                storage.AddBlock(block);
            }

            using (SQLiteBlockchainStorage storage = SQLiteBlockchainStorage.Open(testFolder))
            {
                StoredBlock block = storage.FindBlockByHash(GenesisBlock.Hash);

                Assert.NotNull(block);
                Assert.That(block.Hash, Is.EqualTo(GenesisBlock.Hash));

                block = storage.FindBestHeaderChain();

                Assert.NotNull(block);
                Assert.That(block.Hash, Is.EqualTo(GenesisBlock.Hash));
            }
        }

        [Test]
        public void TestTransactionsWithCache()
        {
            //todo: define specific per storage name when moving to abstract test class
            string testFolder = TestUtils.PrepareTestFolder(typeof(TestSQLiteBlockchainStorage), $"{nameof(TestTransactionsWithCache)}", "*.db");

            using (SQLiteBlockchainStorage storage = SQLiteBlockchainStorage.Open(testFolder))
            {
                CachingBlockchainStorage cache = new CachingBlockchainStorage(storage);

                StoredBlock block = new StoredBlock(GenesisBlock.GetHeader());
                block.IsInBestHeaderChain = true;
                block.IsInBestBlockChain = false;

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

                block.IsInBestBlockChain = true;

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
    }
}