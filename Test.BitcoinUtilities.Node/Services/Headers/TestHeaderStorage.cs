using System.IO;
using System.Linq;
using BitcoinUtilities.Node.Services.Headers;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities.Node.Services.Headers
{
    [TestFixture]
    public class TestHeaderStorage
    {
        [Test]
        public void TestAddRead()
        {
            var headers = new DbHeader[]
            {
                new DbHeader(KnownBlocks.Block100000.Header, KnownBlocks.Block100000.Hash, 0, 100, true),
                new DbHeader(KnownBlocks.Block100001.Header, KnownBlocks.Block100001.Hash, 1, 200, false)
            };

            string testFolder = TestUtils.PrepareTestFolder(this.GetType(), nameof(TestAddRead), "*.db");
            string filename = Path.Combine(testFolder, "headers.db");

            using (HeaderStorage storage1 = HeaderStorage.Open(filename))
            {
                storage1.AddHeaders(headers);
                using (HeaderStorage storage2 = HeaderStorage.Open(filename))
                {
                    var headers2 = storage2.ReadAll();
                    Assert.That(headers2.Select(h => h.AsText).ToArray(), Is.EqualTo(headers.Select(h => h.AsText).ToArray()));
                }
            }
        }

        [Test]
        public void TestMarkInvalid()
        {
            string testFolder = TestUtils.PrepareTestFolder(this.GetType(), nameof(TestMarkInvalid), "*.db");
            string filename = Path.Combine(testFolder, "headers.db");
            using (HeaderStorage storage1 = HeaderStorage.Open(filename))
            {
                var headers = new DbHeader[]
                {
                    new DbHeader(KnownBlocks.Block100000.Header, KnownBlocks.Block100000.Hash, 0, 100, true),
                    new DbHeader(KnownBlocks.Block100001.Header, KnownBlocks.Block100001.Hash, 1, 200, true)
                };

                storage1.AddHeaders(headers);
                storage1.MarkInvalid(new DbHeader[] {headers[1]});
            }

            using (HeaderStorage storage2 = HeaderStorage.Open(filename))
            {
                var headers = new DbHeader[]
                {
                    new DbHeader(KnownBlocks.Block100000.Header, KnownBlocks.Block100000.Hash, 0, 100, true),
                    new DbHeader(KnownBlocks.Block100001.Header, KnownBlocks.Block100001.Hash, 1, 200, false)
                };

                var headers2 = storage2.ReadAll();
                Assert.That(headers2.Select(h => h.AsText).ToArray(), Is.EqualTo(headers.Select(h => h.AsText).ToArray()));
            }
        }
    }
}