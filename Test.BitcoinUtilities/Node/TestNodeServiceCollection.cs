using System;
using System.Diagnostics;
using System.Threading;
using BitcoinUtilities.Node;
using BitcoinUtilities.P2P;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities.Node
{
    [TestFixture]
    public class TestNodeServiceCollection
    {
        [Test]
        public void TestSmoke()
        {
            MessageLog log = new MessageLog();

            NodeServiceCollection services = new NodeServiceCollection();

            TestNodeServiceFactory service1 = new TestNodeServiceFactory(log, TimeSpan.Zero, false);
            TestNodeServiceFactory service2 = new TestNodeServiceFactory(log, TimeSpan.Zero, false);

            services.AddFactory(service1);
            services.AddFactory(service2);

            CancellationTokenSource cts = new CancellationTokenSource();

            services.CreateServices(null, cts.Token);

            Assert.That(log.GetLog(), Is.EqualTo(new string[]
            {
                "Service created.",
                "Service created."
            }));

            services.Start();

            //Sleep to avoid arbitrary order of start and stop events.
            Thread.Sleep(50);

            Assert.That(log.GetLog(), Is.EqualTo(new string[]
            {
                "Service created.",
                "Service created.",
                "Service started.",
                "Service started."
            }));

            cts.Cancel();

            Assert.True(services.Join(TimeSpan.FromMilliseconds(100)));

            Assert.That(log.GetLog(), Is.EqualTo(new string[]
            {
                "Service created.",
                "Service created.",
                "Service started.",
                "Service started.",
                "Service stopped.",
                "Service stopped."
            }));

            services.DisposeServices();

            Assert.That(log.GetLog(), Is.EqualTo(new string[]
            {
                "Service created.",
                "Service created.",
                "Service started.",
                "Service started.",
                "Service stopped.",
                "Service stopped.",
                "Service disposed.",
                "Service disposed."
            }));
        }

        [Test]
        public void TestBrokenDispose()
        {
            MessageLog log = new MessageLog();

            NodeServiceCollection services = new NodeServiceCollection();

            TestNodeServiceFactory service1 = new TestNodeServiceFactory(log, TimeSpan.Zero, true);
            TestNodeServiceFactory service2 = new TestNodeServiceFactory(log, TimeSpan.Zero, true);

            services.AddFactory(service1);
            services.AddFactory(service2);

            CancellationTokenSource cts = new CancellationTokenSource();

            services.CreateServices(null, cts.Token);

            services.Start();

            //Sleep to avoid arbitrary order of start and stop events.
            Thread.Sleep(50);

            cts.Cancel();

            Assert.True(services.Join(TimeSpan.FromMilliseconds(100)));

            services.DisposeServices();

            Assert.That(log.GetLog(), Is.EqualTo(new string[]
            {
                "Service created.",
                "Service created.",
                "Service started.",
                "Service started.",
                "Service stopped.",
                "Service stopped.",
                "Service disposal failed.",
                "Service disposal failed."
            }));
        }

        [Test]
        public void TestSlowCancellation()
        {
            MessageLog log = new MessageLog();

            NodeServiceCollection services = new NodeServiceCollection();

            TestNodeServiceFactory service1 = new TestNodeServiceFactory(log, TimeSpan.FromMilliseconds(200), false);
            TestNodeServiceFactory service2 = new TestNodeServiceFactory(log, TimeSpan.FromMilliseconds(200), false);

            services.AddFactory(service1);
            services.AddFactory(service2);

            CancellationTokenSource cts = new CancellationTokenSource();

            services.CreateServices(null, cts.Token);

            services.Start();

            cts.Cancel();

            Stopwatch sw = Stopwatch.StartNew();

            Assert.False(services.Join(TimeSpan.FromMilliseconds(150)));

            Assert.That(sw.Elapsed, Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(130)));
            Assert.That(sw.Elapsed, Is.LessThanOrEqualTo(TimeSpan.FromMilliseconds(170)));

            Assert.That(log.GetLog(), Is.EqualTo(new string[]
            {
                "Service created.",
                "Service created.",
                "Service started.",
                "Service started."
            }));

            Assert.True(services.Join(TimeSpan.FromMilliseconds(100)));

            Assert.That(sw.Elapsed, Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(180)));
            Assert.That(sw.Elapsed, Is.LessThanOrEqualTo(TimeSpan.FromMilliseconds(220)));

            services.DisposeServices();

            Assert.That(log.GetLog(), Is.EqualTo(new string[]
            {
                "Service created.",
                "Service created.",
                "Service started.",
                "Service started.",
                "Service stopped.",
                "Service stopped.",
                "Service disposed.",
                "Service disposed."
            }));
        }

        private class TestNodeService : INodeService, IDisposable
        {
            private CancellationToken cancellationToken;

            private readonly MessageLog log;
            private readonly TimeSpan shutdownTimeout;

            private readonly bool disposeShouldFail;

            public TestNodeService(MessageLog log, CancellationToken cancellationToken, TimeSpan shutdownTimeout, bool disposeShouldFail)
            {
                this.log = log;
                this.cancellationToken = cancellationToken;
                this.shutdownTimeout = shutdownTimeout;
                this.disposeShouldFail = disposeShouldFail;

                log.Log("Service created.");
            }

            public void Run()
            {
                log.Log("Service started.");

                while (!cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(30)))
                {
                    // just waiting for cancellation   
                }

                Thread.Sleep(shutdownTimeout);

                log.Log("Service stopped.");
            }

            public void OnNodeConnected(BitcoinEndpoint endpoint)
            {
                //todo: test?
            }

            public void OnNodeDisconnected(BitcoinEndpoint endpoint)
            {
                //todo: test?
            }

            public void ProcessMessage(BitcoinEndpoint endpoint, IBitcoinMessage message)
            {
                //todo: add test for this method
            }

            public void Dispose()
            {
                if (disposeShouldFail)
                {
                    log.Log("Service disposal failed.");
                    throw new Exception("Dispose thowing exception as expected.");
                }

                log.Log("Service disposed.");
            }
        }

        public class TestNodeServiceFactory : INodeServiceFactory
        {
            private readonly MessageLog log;
            private readonly TimeSpan shutdownTimeout;
            private readonly bool disposeShouldFail;

            public TestNodeServiceFactory(MessageLog log, TimeSpan shutdownTimeout, bool disposeShouldFail)
            {
                this.log = log;
                this.shutdownTimeout = shutdownTimeout;
                this.disposeShouldFail = disposeShouldFail;
            }

            public INodeService Create(BitcoinNode node, CancellationToken cancellationToken)
            {
                return new TestNodeService(log, cancellationToken, shutdownTimeout, disposeShouldFail);
            }
        }
    }
}