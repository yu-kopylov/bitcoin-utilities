using System.Collections.Generic;
using BitcoinUtilities.Node.Services.Blocks;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities.Node.Services.Blocks
{
    [TestFixture]
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
                storage.UpdateRequests("test", new List<byte[]> {hash});
                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(0));
            }

            using (BlockStorage storage = BlockStorage.Open(testFolder))
            {
                Assert.That(storage.GetBlock(new byte[32]), Is.Null);
                Assert.That(storage.GetBlock(hash), Is.EqualTo(content));
                storage.UpdateRequests("test", new List<byte[]>());
                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(1000));
            }
        }

        [Test]
        public void TestUnrequestedBlocksVolume()
        {
            byte[] hash0 = new byte[32];
            hash0[0] = 0xFF;

            byte[] hash1 = new byte[32];
            hash1[0] = 1;

            byte[] hash2 = new byte[32];
            hash2[0] = 2;

            byte[] hash3 = new byte[32];
            hash3[0] = 3;

            byte[] requestedContent = new byte[333];
            requestedContent[0] = 4;

            byte[] content = new byte[100];
            content[0] = 5;

            string testFolder = TestUtils.PrepareTestFolder(GetType(), nameof(TestUnrequestedBlocksVolume), "*.db");
            using (BlockStorage storage = BlockStorage.Open(testFolder))
            {
                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(0));

                storage.UpdateRequests("test", new List<byte[]> {hash0});
                storage.AddBlock(hash0, requestedContent);
                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(0));
                Assert.That(storage.GetBlock(hash0), Is.EqualTo(requestedContent));
                Assert.That(storage.GetBlock(hash1), Is.Null);
                Assert.That(storage.GetBlock(hash2), Is.Null);
                Assert.That(storage.GetBlock(hash3), Is.Null);

                storage.MaxUnrequestedBlocksVolume = 99;
                storage.AddBlock(hash1, content);
                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(0));
                Assert.That(storage.GetBlock(hash0), Is.EqualTo(requestedContent));
                Assert.That(storage.GetBlock(hash1), Is.Null);
                Assert.That(storage.GetBlock(hash2), Is.Null);
                Assert.That(storage.GetBlock(hash3), Is.Null);

                storage.MaxUnrequestedBlocksVolume = 100;
                storage.AddBlock(hash1, content);
                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(100));
                Assert.That(storage.GetBlock(hash0), Is.EqualTo(requestedContent));
                Assert.That(storage.GetBlock(hash1), Is.EqualTo(content));
                Assert.That(storage.GetBlock(hash2), Is.Null);
                Assert.That(storage.GetBlock(hash3), Is.Null);

                storage.AddBlock(hash2, content);
                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(100));
                Assert.That(storage.GetBlock(hash0), Is.EqualTo(requestedContent));
                Assert.That(storage.GetBlock(hash1), Is.Null);
                Assert.That(storage.GetBlock(hash2), Is.EqualTo(content));
                Assert.That(storage.GetBlock(hash3), Is.Null);

                storage.MaxUnrequestedBlocksVolume = 299;
                storage.AddBlock(hash3, content);
                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(200));
                Assert.That(storage.GetBlock(hash0), Is.EqualTo(requestedContent));
                Assert.That(storage.GetBlock(hash1), Is.Null);
                Assert.That(storage.GetBlock(hash2), Is.EqualTo(content));
                Assert.That(storage.GetBlock(hash3), Is.EqualTo(content));

                storage.AddBlock(hash1, content);
                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(200));
                Assert.That(storage.GetBlock(hash0), Is.EqualTo(requestedContent));
                Assert.That(storage.GetBlock(hash1), Is.EqualTo(content));
                Assert.That(storage.GetBlock(hash2), Is.Null);
                Assert.That(storage.GetBlock(hash3), Is.EqualTo(content));
            }
        }

        [Test]
        public void TestRequestThenAdd()
        {
            byte[] hash1 = new byte[32];
            hash1[0] = 1;

            byte[] hash2 = new byte[32];
            hash2[0] = 2;

            byte[] content = new byte[100];
            content[0] = 5;

            string testFolder = TestUtils.PrepareTestFolder(GetType(), nameof(TestRequestThenAdd), "*.db");
            using (BlockStorage storage = BlockStorage.Open(testFolder))
            {
                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(0));

                storage.UpdateRequests("token1", new List<byte[]> {hash1});
                storage.UpdateRequests("token2", new List<byte[]> {hash1, hash2});
                storage.AddBlock(hash1, content);
                storage.AddBlock(hash2, content);

                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(0));
                Assert.That(storage.GetBlock(hash1), Is.EqualTo(content));
                Assert.That(storage.GetBlock(hash2), Is.EqualTo(content));

                storage.UpdateRequests("token2", new List<byte[]>());

                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(100));
                Assert.That(storage.GetBlock(hash1), Is.EqualTo(content));
                Assert.That(storage.GetBlock(hash2), Is.EqualTo(content));

                storage.UpdateRequests("token1", new List<byte[]>());

                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(200));
                Assert.That(storage.GetBlock(hash1), Is.EqualTo(content));
                Assert.That(storage.GetBlock(hash2), Is.EqualTo(content));
            }
        }

        [Test]
        public void TestAddThenRequest()
        {
            byte[] hash1 = new byte[32];
            hash1[0] = 1;

            byte[] hash2 = new byte[32];
            hash2[0] = 2;

            byte[] content = new byte[100];
            content[0] = 5;

            string testFolder = TestUtils.PrepareTestFolder(GetType(), nameof(TestAddThenRequest), "*.db");
            using (BlockStorage storage = BlockStorage.Open(testFolder))
            {
                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(0));

                storage.AddBlock(hash1, content);
                storage.AddBlock(hash2, content);

                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(200));

                storage.UpdateRequests("token1", new List<byte[]> {hash1});

                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(100));

                storage.UpdateRequests("token2", new List<byte[]> {hash1, hash2});

                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(0));
                Assert.That(storage.GetBlock(hash1), Is.EqualTo(content));
                Assert.That(storage.GetBlock(hash2), Is.EqualTo(content));

                storage.UpdateRequests("token2", new List<byte[]>());

                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(100));
                Assert.That(storage.GetBlock(hash1), Is.EqualTo(content));
                Assert.That(storage.GetBlock(hash2), Is.EqualTo(content));

                storage.UpdateRequests("token1", new List<byte[]>());

                Assert.That(storage.GetUnrequestedVolume(), Is.EqualTo(200));
                Assert.That(storage.GetBlock(hash1), Is.EqualTo(content));
                Assert.That(storage.GetBlock(hash2), Is.EqualTo(content));
            }
        }
    }
}