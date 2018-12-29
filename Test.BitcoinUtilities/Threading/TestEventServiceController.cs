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
                controller.AddService(new PrintService<string>("service s1", messageLog));
                controller.AddService(new PrintService<string>("service s2", messageLog));
                controller.AddService(new PrintService<string>("service s3", messageLog));
                controller.AddService(new PrintService<int>("service i1", messageLog));
                controller.AddService(new PrintService<int>("service i2", messageLog));
                controller.AddService(new PrintService<int>("service i3", messageLog));
                controller.Start();
                controller.Raise("string 1");
                controller.Raise("string 2");
                controller.Raise("string 3");
                controller.Raise(1);
                controller.Raise(2);
                controller.Raise(3);
                Thread.Sleep(100);
                controller.Stop();
            }

            string[] log = messageLog.GetLog();
            for (int i = 1; i <= 3; i++)
            {
                Assert.That(log.Where(m => m.StartsWith($"service s{i}:")).ToArray(), Is.EqualTo(new string[]
                {
                    $"service s{i}: start",
                    $"service s{i}: string 1",
                    $"service s{i}: string 2",
                    $"service s{i}: string 3",
                    $"service s{i}: stop"
                }));

                Assert.That(log.Where(m => m.StartsWith($"service i{i}:")).ToArray(), Is.EqualTo(new string[]
                {
                    $"service i{i}: start",
                    $"service i{i}: 1",
                    $"service i{i}: 2",
                    $"service i{i}: 3",
                    $"service i{i}: stop"
                }));
            }
        }

        private class PrintService<T> : EventHandlingService
        {
            private readonly string name;
            private readonly MessageLog log;

            public PrintService(string name, MessageLog log)
            {
                this.name = name;
                this.log = log;
                On<T>(Print);
            }

            public override void OnStart()
            {
                base.OnStart();
                log.Log(name + ": start");
            }

            public override void OnTearDown()
            {
                base.OnTearDown();
                log.Log(name + ": stop");
            }

            private void Print(T obj)
            {
                log.Log(name + ": " + obj);
            }
        }
    }
}