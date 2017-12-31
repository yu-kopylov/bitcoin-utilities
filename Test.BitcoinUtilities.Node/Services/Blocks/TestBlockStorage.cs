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
            string testFolder = TestUtils.PrepareTestFolder(GetType(), $"{nameof(TestSmoke)}", "*.db");
            using (BlockStorage storage = BlockStorage.Open(testFolder))
            {
                //todo: add some testing
            }
            // reopen created storage
            using (BlockStorage storage = BlockStorage.Open(testFolder))
            {
            }
        }
    }
}