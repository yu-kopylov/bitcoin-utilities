using BitcoinUtilities.Node.Services.Blocks;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities.Node.Services.Blocks
{
    [TestFixture]
    [Category("SqliteTest")]
    public class TestBlockStorage
    {
        [Test]
        public void TestSmoke()
        {
            byte[] hash = new byte[32];
            hash[0] = 1;

            byte[] content = new byte[1000];
            content[0] = 2;

            string testFolder = TestUtils.PrepareTestFolder(GetType(), nameof(TestSmoke), "*.db");
            using (BlockStorage storage = BlockStorage.Open(testFolder))
            {
                storage.AddBlock(hash, content);
            }

            using (BlockStorage storage = BlockStorage.Open(testFolder))
            {
                Assert.That(storage.GetBlock(new byte[32]), Is.Null);
                Assert.That(storage.GetBlock(hash), Is.EqualTo(content));
            }
        }

        [Test]
        public void TestUnrequestedBlocksVolume()
        {
            byte[] hash1 = new byte[32];
            hash1[0] = 1;

            byte[] hash2 = new byte[32];
            hash2[0] = 2;

            byte[] hash3 = new byte[32];
            hash3[0] = 3;

            byte[] content = new byte[1000];
            content[0] = 2;

            string testFolder = TestUtils.PrepareTestFolder(GetType(), nameof(TestUnrequestedBlocksVolume), "*.db");
            using (BlockStorage storage = BlockStorage.Open(testFolder))
            {
                storage.MaxUnrequestedBlocksVolume = 999;
                storage.AddBlock(hash1, content);
                Assert.That(storage.GetBlock(hash1), Is.Null);
                Assert.That(storage.GetBlock(hash2), Is.Null);
                Assert.That(storage.GetBlock(hash3), Is.Null);

                storage.MaxUnrequestedBlocksVolume = 1000;
                storage.AddBlock(hash1, content);
                Assert.That(storage.GetBlock(hash1), Is.EqualTo(content));
                Assert.That(storage.GetBlock(hash2), Is.Null);
                Assert.That(storage.GetBlock(hash3), Is.Null);

                storage.AddBlock(hash2, content);
                Assert.That(storage.GetBlock(hash1), Is.Null);
                Assert.That(storage.GetBlock(hash2), Is.EqualTo(content));
                Assert.That(storage.GetBlock(hash3), Is.Null);

                storage.MaxUnrequestedBlocksVolume = 2999;
                storage.AddBlock(hash3, content);
                Assert.That(storage.GetBlock(hash1), Is.Null);
                Assert.That(storage.GetBlock(hash2), Is.EqualTo(content));
                Assert.That(storage.GetBlock(hash3), Is.EqualTo(content));

                storage.AddBlock(hash1, content);
                Assert.That(storage.GetBlock(hash1), Is.EqualTo(content));
                Assert.That(storage.GetBlock(hash2), Is.Null);
                Assert.That(storage.GetBlock(hash3), Is.EqualTo(content));
            }
        }
    }
}