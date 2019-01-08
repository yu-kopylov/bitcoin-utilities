using System.IO;
using System.Threading;
using BitcoinUtilities;
using BitcoinUtilities.Node.Events;
using BitcoinUtilities.Node.Services.Blocks;
using BitcoinUtilities.Threading;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities.Node.Services.Blocks
{
    [TestFixture]
    public class TestBlockStorageService
    {
        [Test]
        public void Test()
        {
            string testFolder = TestUtils.PrepareTestFolder("*.db");
            string storageFile = Path.Combine(testFolder, "blocks.db");

            using (BlockStorage blockStorage = BlockStorage.Open(storageFile))
            using (EventServiceController controller = new EventServiceController())
            {
                MessageLog log = new MessageLog();
                controller.AddService(new EventLoggingService(log));
                controller.AddService(new BlockStorageService(controller, blockStorage));
                controller.Start();

                log.Clear();
                controller.Raise(new RequestBlockEvent("test", KnownBlocks.Block100000.Hash));
                controller.Raise(new MessageEvent(null, KnownBlocks.Block100001.Block));
                Thread.Sleep(100);

                Assert.AreEqual(new string[0], log.GetLog());

                log.Clear();
                controller.Raise(new MessageEvent(null, KnownBlocks.Block100000.Block));
                Thread.Sleep(100);

                Assert.AreEqual(new string[] {$"BlockAvailableEvent: {HexUtils.GetString(KnownBlocks.Block100000.Hash)}"}, log.GetLog());

                log.Clear();
                controller.Raise(new RequestBlockEvent(null, KnownBlocks.Block100001.Hash));
                Thread.Sleep(100);

                Assert.AreEqual(new string[] {$"BlockAvailableEvent: {HexUtils.GetString(KnownBlocks.Block100001.Hash)}"}, log.GetLog());

                controller.Stop();
            }
        }

        private class EventLoggingService : EventHandlingService
        {
            public EventLoggingService(MessageLog log)
            {
                On<BlockAvailableEvent>(e => log.Log($"BlockAvailableEvent: {HexUtils.GetString(e.HeaderHash)}"));
            }
        }
    }
}