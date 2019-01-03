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
    public class TestHeaderStorage
    {
        [Test]
        public void TestSmoke()
        {
            var block100000 = BitcoinStreamReader.FromBytes(KnownBlocks.Block100000, BlockMessage.Read);
            var block100001 = BitcoinStreamReader.FromBytes(KnownBlocks.Block100001, BlockMessage.Read);
            var headers = new DbHeader[]
            {
                new DbHeader(block100000.BlockHeader, CryptoUtils.DoubleSha256(KnownBlocks.Block100000), 100000, 100),
                new DbHeader(block100001.BlockHeader, CryptoUtils.DoubleSha256(KnownBlocks.Block100001), 100001, 200),
            };

            string testFolder = TestUtils.PrepareTestFolder(this.GetType(), nameof(TestSmoke), "*.db");
            string filename = Path.Combine(testFolder, "headers.db");

            using (HeaderStorage storage1 = HeaderStorage.Open(filename))
            {
                storage1.AddHeaders(headers);
                using (HeaderStorage storage2 = HeaderStorage.Open(filename))
                {
                    var headers2 = storage2.ReadAll();
                    Assert.That(headers2.Select(FormatHeader).ToArray(), Is.EqualTo(headers.Select(FormatHeader).ToArray()));
                }
            }
        }

        private string FormatHeader(DbHeader header)
        {
            return $"Hash:\t{HexUtils.GetString(header.Hash)}\r\n" +
                   $"Height:\t{header.Height}\r\n" +
                   $"TotalWork:\t{header.TotalWork}\r\n" +
                   $"Header:\t{HexUtils.GetString(BitcoinStreamWriter.GetBytes(header.Header.Write))}";
        }
    }
}