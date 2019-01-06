using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BitcoinUtilities;
using BitcoinUtilities.Node.Services.Headers;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities.Node.Services.Headers
{
    [TestFixture]
    public class TestBlockchain
    {
        private readonly SampleBlockGenerator blockGenerator = new SampleBlockGenerator();

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
            SampleTree sampleTree = GenerateSampleTree();

            string testFolder = TestUtils.PrepareTestFolder("*.db");
            string filename = Path.Combine(testFolder, "headers.db");
            using (HeaderStorage storage = HeaderStorage.Open(filename))
            {
                Blockchain2 blockchain = new Blockchain2(storage, sampleTree["a0"].Header);
                blockchain.Add(sampleTree.GetBlocks("a1", "a2", "a3", "a4", "a5"));
                blockchain.Add(sampleTree.GetBlocks("b5"));
                blockchain.Add(sampleTree.GetBlocks("c3", "c4", "c5"));

                Assert.That(blockchain.GetSubChain(sampleTree["d5"].Hash, 10), Is.Null);

                Assert.AreEqual(
                    blockchain.GetSubChain(sampleTree["a0"].Hash, 10).Select(sampleTree.GetName),
                    new string[] {"a0"}
                );

                Assert.AreEqual(
                    blockchain.GetSubChain(sampleTree["a4"].Hash, 10).Select(sampleTree.GetName),
                    new string[] {"a0", "a1", "a2", "a3", "a4"}
                );

                Assert.AreEqual(
                    blockchain.GetSubChain(sampleTree["c4"].Hash, 10).Select(sampleTree.GetName),
                    new string[] {"a0", "a1", "a2", "c3", "c4"}
                );

                Assert.AreEqual(
                    blockchain.GetSubChain(sampleTree["a4"].Hash, 5).Select(sampleTree.GetName),
                    new string[] {"a0", "a1", "a2", "a3", "a4"}
                );

                Assert.AreEqual(
                    blockchain.GetSubChain(sampleTree["c4"].Hash, 5).Select(sampleTree.GetName),
                    new string[] {"a0", "a1", "a2", "c3", "c4"}
                );

                Assert.AreEqual(
                    blockchain.GetSubChain(sampleTree["a4"].Hash, 4).Select(sampleTree.GetName),
                    new string[] {"a1", "a2", "a3", "a4"}
                );

                Assert.AreEqual(
                    blockchain.GetSubChain(sampleTree["c4"].Hash, 4).Select(sampleTree.GetName),
                    new string[] {"a1", "a2", "c3", "c4"}
                );
            }
        }

        [Test]
        public void TestBestHeadSelection()
        {
            SampleTree sampleTree = GenerateSampleTree();

            string testFolder = TestUtils.PrepareTestFolder("*.db");
            string filename = Path.Combine(testFolder, "headers.db");
            using (HeaderStorage storage = HeaderStorage.Open(filename))
            {
                Blockchain2 blockchain = new Blockchain2(storage, sampleTree["a0"].Header);
                Assert.That(blockchain.GetValidHeads().Select(sampleTree.GetName), Is.EquivalentTo(new string[] {"a0"}));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead()), Is.EqualTo("a0"));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["a3"].Hash)), Is.Null);
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["c3"].Hash)), Is.Null);

                blockchain.Add(sampleTree.GetBlocks("a1", "a2", "a3", "a4", "a5"));
                Assert.That(blockchain.GetValidHeads().Select(sampleTree.GetName), Is.EquivalentTo(new string[] {"a5"}));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead()), Is.EqualTo("a5"));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["a3"].Hash)), Is.EqualTo("a5"));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["c3"].Hash)), Is.Null);

                blockchain.Add(sampleTree.GetBlocks("c3", "c4", "c5"));
                Assert.That(blockchain.GetValidHeads().Select(sampleTree.GetName), Is.EquivalentTo(new string[] {"a5", "c5"}));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead()), Is.EqualTo("c5"));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["a3"].Hash)), Is.EqualTo("a5"));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["c3"].Hash)), Is.EqualTo("c5"));

                blockchain.Add(sampleTree.GetBlocks("d5"));
                Assert.That(blockchain.GetValidHeads().Select(sampleTree.GetName), Is.EquivalentTo(new string[] {"a5", "c5", "d5"}));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead()), Is.EqualTo("c5"));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["a3"].Hash)), Is.EqualTo("a5"));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["c3"].Hash)), Is.EqualTo("c5"));

                blockchain.Add(sampleTree.GetBlocks("b5"));
                Assert.That(blockchain.GetValidHeads().Select(sampleTree.GetName), Is.EquivalentTo(new string[] {"a5", "b5", "c5", "d5"}));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead()), Is.EqualTo("b5"));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["a3"].Hash)), Is.EqualTo("b5"));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["c3"].Hash)), Is.EqualTo("c5"));

                blockchain.MarkInvalid(sampleTree["b5"].Hash);
                Assert.That(blockchain.GetValidHeads().Select(sampleTree.GetName), Is.EquivalentTo(new string[] {"a5", "c5", "d5"}));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead()), Is.EqualTo("c5"));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["a3"].Hash)), Is.EqualTo("a5"));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["c3"].Hash)), Is.EqualTo("c5"));

                blockchain.MarkInvalid(sampleTree["c4"].Hash);
                Assert.That(blockchain.GetValidHeads().Select(sampleTree.GetName), Is.EquivalentTo(new string[] {"a5", "c3"}));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead()), Is.EqualTo("a5"));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["a3"].Hash)), Is.EqualTo("a5"));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["c3"].Hash)), Is.EqualTo("c3"));

                blockchain.MarkInvalid(sampleTree["a2"].Hash);
                Assert.That(blockchain.GetValidHeads().Select(sampleTree.GetName), Is.EquivalentTo(new string[] {"a1"}));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead()), Is.EqualTo("a1"));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["a3"].Hash)), Is.Null);
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["c3"].Hash)), Is.Null);

                // markilg block that is already invalid
                blockchain.MarkInvalid(sampleTree["a2"].Hash);
                Assert.That(blockchain.GetValidHeads().Select(sampleTree.GetName), Is.EquivalentTo(new string[] {"a1"}));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead()), Is.EqualTo("a1"));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["a3"].Hash)), Is.Null);
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["c3"].Hash)), Is.Null);

                // markilg non-existing block
                blockchain.MarkInvalid(new byte[32]);
                Assert.That(blockchain.GetValidHeads().Select(sampleTree.GetName), Is.EquivalentTo(new string[] {"a1"}));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead()), Is.EqualTo("a1"));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["a3"].Hash)), Is.Null);
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["c3"].Hash)), Is.Null);

                blockchain.MarkInvalid(sampleTree["a1"].Hash);
                Assert.That(blockchain.GetValidHeads().Select(sampleTree.GetName), Is.EquivalentTo(new string[] {"a0"}));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead()), Is.EqualTo("a0"));
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["a3"].Hash)), Is.Null);
                Assert.That(sampleTree.GetName(blockchain.GetBestHead(sampleTree["c3"].Hash)), Is.Null);

                Assert.That(
                    Assert.Throws<InvalidOperationException>(() => blockchain.MarkInvalid(sampleTree["a0"].Hash)).Message,
                    Is.EqualTo("The root of the blockchain cannot be marked as invalid.")
                );
            }
        }

        ///<summary>
        /// <p>Generates a tree with the following structure:</p>
        /// <p><c>a0(0) - a1(10) -> a2(20) -> a3(30) -> a4(40) -> a5(50)</c></p>
        /// <p><c>``````````````````` | ````````````````` \ ----> b5(55)</c></p>
        /// <p><c>``````````````````` |                                 </c></p>
        /// <p><c>``````````````````` \ ----> c3(31) -> c4(42) -> c5(53)</c></p>
        /// <p><c>``````````````````````````````````````` \ ----> d5(52)</c></p>
        /// <p>All blocks are valid.</p>
        /// <p>Total work is listed in brakets.</p>
        ///</summary>
        private SampleTree GenerateSampleTree()
        {
            SampleTree sampleTree = new SampleTree();
            var headersA = GenerateHeaders(6, null, 10);
            var headersB = GenerateHeaders(1, headersA[4], 15);
            var headersC = GenerateHeaders(3, headersA[2], 11);
            var headersD = GenerateHeaders(1, headersC[1], 10);

            sampleTree.Add(headersA, "a", 0);
            sampleTree.Add(headersB, "b", 5);
            sampleTree.Add(headersC, "c", 3);
            sampleTree.Add(headersD, "d", 5);

            Assert.That(sampleTree.GetBlocks("a5")[0].TotalWork, Is.EqualTo(50d));
            Assert.That(sampleTree.GetBlocks("b5")[0].TotalWork, Is.EqualTo(55d));
            Assert.That(sampleTree.GetBlocks("c5")[0].TotalWork, Is.EqualTo(53d));
            Assert.That(sampleTree.GetBlocks("d5")[0].TotalWork, Is.EqualTo(52d));

            Assert.That(sampleTree.GetBlocks("a5")[0].Height, Is.EqualTo(5));
            Assert.That(sampleTree.GetBlocks("b5")[0].Height, Is.EqualTo(5));
            Assert.That(sampleTree.GetBlocks("c5")[0].Height, Is.EqualTo(5));
            Assert.That(sampleTree.GetBlocks("d5")[0].Height, Is.EqualTo(5));

            return sampleTree;
        }

        private List<DbHeader> GenerateHeaders(int count, DbHeader prevHeader, double workPerBlock)
        {
            var blocks = blockGenerator.GenerateChain(prevHeader?.Hash ?? new byte[32], count);

            int nextHeight = prevHeader == null ? 0 : prevHeader.Height + 1;
            double nextTotalWork = prevHeader == null ? 0 : prevHeader.TotalWork + workPerBlock;

            List<DbHeader> headers = new List<DbHeader>(count);
            foreach (BlockMessage block in blocks)
            {
                headers.Add(new DbHeader(
                    block.BlockHeader,
                    CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(block.BlockHeader.Write)),
                    nextHeight,
                    nextTotalWork,
                    true
                ));
                nextHeight++;
                nextTotalWork += workPerBlock;
            }

            return headers;
        }

        private class SampleTree
        {
            private readonly Dictionary<string, DbHeader> headers = new Dictionary<string, DbHeader>();
            private readonly Dictionary<byte[], string> blockNames = new Dictionary<byte[], string>(ByteArrayComparer.Instance);

            public void Add(List<DbHeader> headera, string chainPrefix, int firstIndex)
            {
                int index = firstIndex;
                foreach (var header in headera)
                {
                    Add(chainPrefix + index, header);
                    index++;
                }
            }

            private void Add(string blockName, DbHeader header)
            {
                headers.Add(blockName, header);
                blockNames.Add(header.Hash, blockName);
            }

            public DbHeader this[string blockName] => headers[blockName];

            public List<DbHeader> GetBlocks(params string[] blockNames)
            {
                return blockNames.Select(name => headers[name]).ToList();
            }

            public string GetName(DbHeader header)
            {
                if (header == null)
                {
                    return null;
                }

                return blockNames[header.Hash];
            }
        }
    }
}