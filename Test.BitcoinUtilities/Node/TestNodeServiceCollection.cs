using System;
using System.Diagnostics;
using System.Threading;
using BitcoinUtilities.Node;
using BitcoinUtilities.P2P;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Node
{
    [TestFixture]
    public class TestNodeServiceCollection
    {
        [Test]
        public void TestBrokenDispose()
        {
            NodeServiceCollection services = new NodeServiceCollection();

            TestNodeService service1 = new TestNodeService(TimeSpan.Zero, true);
            TestNodeService service2 = new TestNodeService(TimeSpan.Zero, true);

            services.Add(service1);
            services.Add(service2);

            CancellationTokenSource cts = new CancellationTokenSource();

            services.Init(null, cts.Token);

            services.Start();

            cts.Cancel();

            Assert.True(services.Join(TimeSpan.FromMilliseconds(100)));

            Assert.False(service1.Disposed);
            Assert.False(service2.Disposed);

            services.Dispose();

            Assert.True(service1.Disposed);
            Assert.True(service2.Disposed);
        }

        [Test]
        public void TestSlowCancellation()
        {
            NodeServiceCollection services = new NodeServiceCollection();

            TestNodeService service1 = new TestNodeService(TimeSpan.FromMilliseconds(200), false);
            TestNodeService service2 = new TestNodeService(TimeSpan.FromMilliseconds(200), false);

            services.Add(service1);
            services.Add(service2);

            CancellationTokenSource cts = new CancellationTokenSource();

            services.Init(null, cts.Token);

            services.Start();

            cts.Cancel();

            Stopwatch sw = Stopwatch.StartNew();

            Assert.False(services.Join(TimeSpan.FromMilliseconds(150)));

            Assert.That(sw.Elapsed, Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(130)));
            Assert.That(sw.Elapsed, Is.LessThanOrEqualTo(TimeSpan.FromMilliseconds(170)));

            Assert.True(services.Join(TimeSpan.FromMilliseconds(100)));

            Assert.That(sw.Elapsed, Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(180)));
            Assert.That(sw.Elapsed, Is.LessThanOrEqualTo(TimeSpan.FromMilliseconds(220)));

            services.Dispose();

            Assert.True(service1.Disposed);
            Assert.True(service2.Disposed);
        }

        private class TestNodeService : INodeService, IDisposable
        {
            private CancellationToken cancellationToken;

            private readonly TimeSpan shutdownTimeout;

            private readonly bool disposeShouldFail;

            private volatile bool disposed;

            public TestNodeService(TimeSpan shutdownTimeout, bool disposeShouldFail)
            {
                this.shutdownTimeout = shutdownTimeout;
                this.disposeShouldFail = disposeShouldFail;
            }

            public bool Disposed
            {
                get { return disposed; }
            }

            public void Init(BitcoinNode node, CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
            }

            public void Run()
            {
                while (!cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(30)))
                {
                    // just waiting for cancellation   
                }

                Thread.Sleep(shutdownTimeout);
            }

            public void ProcessMessage(IBitcoinMessage message)
            {
                //todo: add test for this method
            }

            public void Dispose()
            {
                disposed = true;

                if (disposeShouldFail)
                {
                    throw new Exception("Dispose thowing exception as expected.");
                }
            }
        }
    }
}