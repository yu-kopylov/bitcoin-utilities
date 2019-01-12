using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BitcoinUtilities;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Node.Services.Headers;
using BitcoinUtilities.Node.Services.Outputs;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Threading;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities.Node.Services.Outputs
{
    [TestFixture]
    public class TestUtxoUpdateService
    {
        [Test]
        public void Test()
        {
            string testFolder = TestUtils.PrepareTestFolder("*.db");
            string blockchainFile = Path.Combine(testFolder, "headers.db");
            string utxoFile = Path.Combine(testFolder, "utxo.db");

            SampleBlockGenerator blockGenerator = new SampleBlockGenerator();
            List<BlockMessage> blocks = blockGenerator.GenerateChain(new byte[0], 5);
            List<DbHeader> headers = new List<DbHeader>();
            for (int i = 0; i < blocks.Count; i++)
            {
                BlockMessage block = blocks[i];
                headers.Add(new DbHeader(
                    block.BlockHeader,
                    CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(block.BlockHeader.Write)),
                    i, i, true
                ));
            }

            using (HeaderStorage headerStorage = HeaderStorage.Open(blockchainFile))
            using (UtxoStorage utxoStorage = UtxoStorage.Open(utxoFile))
            using (EventServiceController controller = new EventServiceController())
            {
                var blockchain = new Blockchain2(headerStorage, blocks[0].BlockHeader);
                blockchain.Add(headers);

                MessageLog log = new MessageLog();
                controller.AddService(new EventLoggingService(log));
                controller.AddService(new UtxoUpdateService(controller, blockchain, utxoStorage));
                controller.Start();
                Thread.Sleep(100);

                Assert.AreEqual(new string[]
                {
                    $"PrefetchBlocksEvent: Headers[5] ({HexUtils.GetString(headers[0].Hash)})",
                    $"RequestBlockEvent: {HexUtils.GetString(headers[0].Hash)}"
                }, log.GetLog());

                log.Clear();
                controller.Raise(new BlockAvailableEvent(headers[0].Hash, blocks[0]));
                Thread.Sleep(100);

                Assert.AreEqual(new string[]
                {
                    $"PrefetchBlocksEvent: Headers[4] ({HexUtils.GetString(headers[1].Hash)})",
                    $"RequestBlockEvent: {HexUtils.GetString(headers[1].Hash)}"
                }, log.GetLog());

                log.Clear();
                controller.Raise(new BlockAvailableEvent(headers[1].Hash, blocks[1]));
                Thread.Sleep(100);

                Assert.AreEqual(new string[]
                {
                    $"PrefetchBlocksEvent: Headers[3] ({HexUtils.GetString(headers[2].Hash)})",
                    $"RequestBlockEvent: {HexUtils.GetString(headers[2].Hash)}"
                }, log.GetLog());

                // todo: select by txHash?
                var outPoints = blocks.Take(2)
                    .SelectMany(b => b.Transactions.Select(tx => new
                    {
                        tx,
                        txHash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(tx.Write))
                    }))
                    .SelectMany(tx => tx.tx.Outputs.Select((o, i) => new TxOutPoint(tx.txHash, i)))
                    .ToList();

                var unspentOutputs = utxoStorage.GetUnspentOutputs(outPoints);

                Assume.That(outPoints.Count, Is.EqualTo(2));
                Assert.That(unspentOutputs.Count, Is.EqualTo(outPoints.Count));

                controller.Stop();
            }
        }

        private class EventLoggingService : EventHandlingService
        {
            public EventLoggingService(MessageLog log)
            {
                On<PrefetchBlocksEvent>(e => log.Log($"PrefetchBlocksEvent: Headers[{e.Headers.Count}] ({HexUtils.GetString(e.Headers.FirstOrDefault()?.Hash)})"));
                On<RequestBlockEvent>(e => log.Log($"RequestBlockEvent: {HexUtils.GetString(e.Hash)}"));
            }
        }
    }
}