﻿using System.IO;
using System.Linq;
using BitcoinUtilities.Node.Services.Headers;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities.Node.Services.Headers
{
    [TestFixture]
    public class TestBlockchain
    {
        [Test]
        public void TestSmoke()
        {
            string testFolder = TestUtils.PrepareTestFolder("*.db");
            string filename = Path.Combine(testFolder, "headers.db");
            using (HeaderStorage storage = HeaderStorage.Open(filename))
            {
                var headers = new DbHeader[]
                {
                    new DbHeader(KnownBlocks.Block100000.Header, KnownBlocks.Block100000.Hash, 0, 0, true),
                    new DbHeader(KnownBlocks.Block100001.Header, KnownBlocks.Block100001.Hash, 1, 100, true),
                    new DbHeader(KnownBlocks.Block100002.Header, KnownBlocks.Block100002.Hash, 2, 200, true)
                };

                Blockchain2 blockchain = new Blockchain2(storage, KnownBlocks.Block100000.Header);
                Assert.That(blockchain.Add(headers).Count, Is.EqualTo(3));
                blockchain.MarkInvalid(headers[1].Hash);
            }

            using (HeaderStorage storage = HeaderStorage.Open(filename))
            {
                var headers = new DbHeader[]
                {
                    new DbHeader(KnownBlocks.Block100000.Header, KnownBlocks.Block100000.Hash, 0, 0, true),
                    new DbHeader(KnownBlocks.Block100001.Header, KnownBlocks.Block100001.Hash, 1, 100, false),
                    new DbHeader(KnownBlocks.Block100002.Header, KnownBlocks.Block100002.Hash, 2, 200, false)
                };

                Blockchain2 blockchain = new Blockchain2(storage, KnownBlocks.Block100000.Header);
                var headers2 = blockchain.GetHeaders(new byte[][]
                {
                    KnownBlocks.Block1.Hash,
                    KnownBlocks.Block100000.Hash,
                    KnownBlocks.Block100001.Hash,
                    KnownBlocks.Block100002.Hash
                });
                Assert.That(headers2.Select(h => h.AsText).ToArray(), Is.EqualTo(headers.Select(h => h.AsText).ToArray()));
            }
        }

        [Test]
        public void TestSaveWithInvalid()
        {
            string testFolder = TestUtils.PrepareTestFolder("*.db");
            string filename = Path.Combine(testFolder, "headers.db");
            using (HeaderStorage storage = HeaderStorage.Open(filename))
            {
                var headers = new DbHeader[]
                {
                    new DbHeader(KnownBlocks.Block100000.Header, KnownBlocks.Block100000.Hash, 0, 0, true),
                    new DbHeader(KnownBlocks.Block100001.Header, KnownBlocks.Block100001.Hash, 1, 100, false),
                    new DbHeader(KnownBlocks.Block100002.Header, KnownBlocks.Block100002.Hash, 2, 200, true)
                };

                Blockchain2 blockchain = new Blockchain2(storage, KnownBlocks.Block100000.Header);
                blockchain.Add(headers);
                var foundHeaders = blockchain.GetHeaders(new byte[][]
                {
                    KnownBlocks.Block100000.Hash,
                    KnownBlocks.Block100001.Hash,
                    KnownBlocks.Block100002.Hash
                });

                var expectedHeaders = new DbHeader[]
                {
                    new DbHeader(KnownBlocks.Block100000.Header, KnownBlocks.Block100000.Hash, 0, 0, true),
                    new DbHeader(KnownBlocks.Block100001.Header, KnownBlocks.Block100001.Hash, 1, 100, false),
                    new DbHeader(KnownBlocks.Block100002.Header, KnownBlocks.Block100002.Hash, 2, 200, false)
                };
                Assert.That(foundHeaders.Select(h => h.AsText).ToArray(), Is.EqualTo(expectedHeaders.Select(h => h.AsText).ToArray()));
            }
        }

        [Test]
        public void TestGetSubChain()
        {
            string testFolder = TestUtils.PrepareTestFolder("*.db");
            string filename = Path.Combine(testFolder, "headers.db");
            using (HeaderStorage storage = HeaderStorage.Open(filename))
            {
                var headers = new DbHeader[]
                {
                    new DbHeader(KnownBlocks.Block100000.Header, KnownBlocks.Block100000.Hash, 0, 0, true),
                    new DbHeader(KnownBlocks.Block100001.Header, KnownBlocks.Block100001.Hash, 1, 100, true),
                    new DbHeader(KnownBlocks.Block100002.Header, KnownBlocks.Block100002.Hash, 2, 200, true)
                };

                Blockchain2 blockchain = new Blockchain2(storage, KnownBlocks.Block100000.Header);
                Assert.That(blockchain.Add(headers).Count, Is.EqualTo(3));

                Assert.That(blockchain.GetSubChain(KnownBlocks.Block1.Hash, 1), Is.Null);

                Assert.That(blockchain.GetSubChain(KnownBlocks.Block100000.Hash, 10)?.Select(h => h.Hash).ToArray(), Is.EqualTo(
                    new byte[][] {KnownBlocks.Block100000.Hash}
                    //todo: finish test
                ));
            }
        }
    }
}