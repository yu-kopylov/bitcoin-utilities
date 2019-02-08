using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BitcoinUtilities;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Node.Services.Headers;
using BitcoinUtilities.Node.Services.Outputs;
using BitcoinUtilities.Node.Services.Outputs.Events;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
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
                var blockchain = new Blockchain(headerStorage, blocks[0].BlockHeader);
                blockchain.Add(headers);

                var utxoUpdateService = new UtxoUpdateService(controller, blockchain, utxoStorage);

                MessageLog log = new MessageLog();
                controller.AddService(new EventLoggingService(log));
                controller.AddService(utxoUpdateService);
                controller.AddService(new SignatureValidationService(controller));
                controller.Start();
                Thread.Sleep(100);

                Assert.That(log.GetLog(), Is.EquivalentTo(new string[]
                {
                    $"PrefetchBlocksEvent: Headers[5] ({HexUtils.GetString(headers[0].Hash)})",
                    $"RequestBlockEvent: {HexUtils.GetString(headers[0].Hash)}"
                }));

                log.Clear();
                controller.Raise(new BlockAvailableEvent(headers[0].Hash, blocks[0]));
                Thread.Sleep(100);

                Assert.That(log.GetLog(), Is.EquivalentTo(new string[]
                {
                    $"SignatureValidationRequest: {HexUtils.GetString(headers[0].Hash)}",
                    $"PrefetchBlocksEvent: Headers[4] ({HexUtils.GetString(headers[1].Hash)})",
                    $"RequestBlockEvent: {HexUtils.GetString(headers[1].Hash)}",
                    $"SignatureValidationResponse: {HexUtils.GetString(headers[0].Hash)}, True"
                }));

                log.Clear();
                controller.Raise(new BlockAvailableEvent(headers[1].Hash, blocks[1]));
                Thread.Sleep(100);

                Assert.That(log.GetLog(), Is.EquivalentTo(new string[]
                {
                    $"SignatureValidationRequest: {HexUtils.GetString(headers[1].Hash)}",
                    $"PrefetchBlocksEvent: Headers[3] ({HexUtils.GetString(headers[2].Hash)})",
                    $"RequestBlockEvent: {HexUtils.GetString(headers[2].Hash)}",
                    $"SignatureValidationResponse: {HexUtils.GetString(headers[1].Hash)}, True"
                }));

                utxoUpdateService.SaveValidatedUpdates();

                var expectedOutputs = blocks
                    .Take(2)
                    .SelectMany(b => b.Transactions)
                    .SelectMany(tx => tx.Outputs)
                    .ToList();

                var allTxHashes = blocks
                    .SelectMany(b => b.Transactions)
                    .Select(tx => tx.Hash)
                    .ToList();

                var unspentOutputs = utxoStorage.GetUnspentOutputs(allTxHashes);

                Assume.That(expectedOutputs.Count, Is.EqualTo(2));
                Assert.That(unspentOutputs.Count, Is.EqualTo(expectedOutputs.Count));

                controller.Stop();
            }
        }

        private class EventLoggingService : EventHandlingService
        {
            public EventLoggingService(MessageLog log)
            {
                On<PrefetchBlocksEvent>(e => log.Log($"PrefetchBlocksEvent: Headers[{e.Headers.Count}] ({HexUtils.GetString(e.Headers.FirstOrDefault()?.Hash)})"));
                On<RequestBlockEvent>(e => log.Log($"RequestBlockEvent: {HexUtils.GetString(e.Hash)}"));
                On<SignatureValidationRequest>(e => log.Log($"SignatureValidationRequest: {HexUtils.GetString(e.Header.Hash)}"));
                On<SignatureValidationResponse>(e => log.Log($"SignatureValidationResponse: {HexUtils.GetString(e.Header.Hash)}, {e.Valid}"));
            }
        }
    }
}