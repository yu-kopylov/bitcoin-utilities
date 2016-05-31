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
                StoredBlock block = new StoredBlock(GenesisBlock.GetHeader());
                Assert.Null(storage.FindBlockByHash(GenesisBlock.Hash));
                storage.AddBlock(block);
            }

            using (SQLiteBlockchainStorage storage = SQLiteBlockchainStorage.Open(testFolder))
            {
                StoredBlock block = storage.FindBlockByHash(GenesisBlock.Hash);
                Assert.NotNull(block);
                Assert.That(block.Hash, Is.EqualTo(GenesisBlock.Hash));
            }
        }
    }
}