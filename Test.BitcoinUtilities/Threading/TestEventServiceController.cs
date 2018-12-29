using System;
using System.Linq;
using System.Threading;
using BitcoinUtilities.Threading;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities.Threading
{
    [TestFixture]
    public class TestEventServiceController
    {
        // todo: add more tests

        [Test]
        public void Test()
        {
            var messageLog = new MessageLog();
            using (EventServiceController controller = new EventServiceController())
            {
                controller.AddService(new StringPrintService("service 1", messageLog));
                controller.AddService(new StringPrintService("service 2", messageLog));
                controller.AddService(new StringPrintService("service 3", messageLog));
                controller.Start();
                controller.Raise("string 1");
                controller.Raise("string 2");
                controller.Raise("string 3");
                Thread.Sleep(100);
                controller.Stop();
            }

            string[] log = messageLog.GetLog();
            Assert.That(log.Where(m => m.StartsWith("service 1:")).ToArray(), Is.EqualTo(new string[]
            {
                "service 1: string 1",
                "service 1: string 2",
                "service 1: string 3"
            }));
            Assert.That(log.Where(m => m.StartsWith("service 2:")).ToArray(), Is.EqualTo(new string[]
            {
                "service 2: string 1",
                "service 2: string 2",
                "service 2: string 3"
            }));
            Assert.That(log.Where(m => m.StartsWith("service 3:")).ToArray(), Is.EqualTo(new string[]
            {
                "service 3: string 1",
                "service 3: string 2",
                "service 3: string 3"
            }));
        }

        private class StringPrintService : IEventHandlingService
        {
            private readonly string name;
            private readonly MessageLog log;

            public StringPrintService(string name, MessageLog log)
            {
                this.name = name;
                this.log = log;
            }

            public Action<object> GetHandler(object evt)
            {
                if (evt is string)
                {
                    return Print;
                }

                return null;
            }

            private void Print(object obj)
            {
                log.Log(name + ": " + (string) obj);
            }
        }
    }
}