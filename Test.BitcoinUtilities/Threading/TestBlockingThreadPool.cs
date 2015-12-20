using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using BitcoinUtilities.Threading;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Threading
{
    [TestFixture]
    public class TestBlockingThreadPool
    {
        private const int TestIterationsCount = 3;

        [TestFixtureSetUp]
        public void WarmUp()
        {
            //the first thread creation can take considerable time
            Stopwatch sw = Stopwatch.StartNew();
            AutoResetEvent ev = new AutoResetEvent(false);
            BlockingThreadPool threadPool = new BlockingThreadPool(1, 1);
            Assume.That(threadPool.Execute(() => ev.Set(), 100), Is.True);
            ev.WaitOne();
            Assert.That(threadPool.Stop(1000), Is.True);
            Console.WriteLine("WarmUp: {0}ms", sw.ElapsedMilliseconds);
        }

        [Test]
        public void TestNoBlock()
        {
            BlockingThreadPool threadPool = new BlockingThreadPool(2, 2);
            MessageLog log = new MessageLog();

            for (int i = 0; i < TestIterationsCount; i++)
            {
                log.Clear();

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 1: started"); // T = 0
                                                   Thread.Sleep(200);
                                                   log.Log("task 1: finished"); // T = 200
                                               }, 100), Is.True);

                Thread.Sleep(50); // T = 50

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 2: started"); // T = 50
                                                   Thread.Sleep(200);
                                                   log.Log("task 2: finished"); // T = 250
                                               }, 100), Is.True);

                Thread.Sleep(50);
                log.Log("main: wait started"); // T = 100
                Thread.Sleep(200);
                log.Log("main: wait finished"); // T = 300

                Assert.That(log.GetLog(), Is.EqualTo(new string[]
                                                     {
                                                         "task 1: started",
                                                         "task 2: started",
                                                         "main: wait started",
                                                         "task 1: finished",
                                                         "task 2: finished",
                                                         "main: wait finished"
                                                     }));
            }

            Assert.That(threadPool.Stop(1000), Is.True);
        }

        [Test]
        public void TestWithBlock()
        {
            BlockingThreadPool threadPool = new BlockingThreadPool(2, 2);
            MessageLog log = new MessageLog();

            for (int i = 0; i < TestIterationsCount; i++)
            {
                log.Clear();

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 1: started"); // T = 0
                                                   Thread.Sleep(250);
                                                   log.Log("task 1: finished"); // T = 250
                                               }, 100), Is.True);

                Thread.Sleep(50); // T = 50

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 2: started"); // T = 50
                                                   Thread.Sleep(350);
                                                   log.Log("task 2: finished"); // T = 400
                                               }, 100), Is.True);

                Thread.Sleep(50); // T = 100

                Assert.That(threadPool.Execute(() => { log.Log("task 3: excuted"); }, 100), Is.False); // T = 200

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 4: started"); // T = 250
                                                   Thread.Sleep(100);
                                                   log.Log("task 4: finished"); // T = 350
                                               }, 100), Is.True); // T = 250

                Thread.Sleep(50);
                log.Log("main: wait started"); // T = 300
                Thread.Sleep(150);
                log.Log("main: wait finished"); // T = 450

                Assert.That(log.GetLog(), Is.EqualTo(new string[]
                                                     {
                                                         "task 1: started",
                                                         "task 2: started",
                                                         "task 1: finished",
                                                         "task 4: started",
                                                         "main: wait started",
                                                         "task 4: finished",
                                                         "task 2: finished",
                                                         "main: wait finished"
                                                     }));
            }

            Assert.That(threadPool.Stop(1000), Is.True);
        }

        [Test]
        public void TestQueuedTasks()
        {
            BlockingThreadPool threadPool = new BlockingThreadPool(2, 3);
            MessageLog log = new MessageLog();

            for (int i = 0; i < TestIterationsCount; i++)
            {
                log.Clear();

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 1: started"); // T = 0
                                                   Thread.Sleep(300);
                                                   log.Log("task 1: finished"); // T = 300
                                               }, 100), Is.True);

                Thread.Sleep(50); // T = 50 

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 2: started"); // T = 50
                                                   Thread.Sleep(500);
                                                   log.Log("task 2: finished"); // T = 550
                                               }, 100), Is.True);

                Thread.Sleep(50); // T = 100 

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 3: started"); // T = 300
                                                   Thread.Sleep(300);
                                                   log.Log("task 3: finished"); // T = 600
                                               }, 100), Is.True);

                Thread.Sleep(50); // T = 150 

                Assert.That(threadPool.Execute(() => { log.Log("task 4: executed"); }, 100), Is.False); // T = 250

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 5: started"); // T = 550
                                                   Thread.Sleep(100);
                                                   log.Log("task 5: finished"); // T = 650
                                               }, 100), Is.True); // T = 300

                Thread.Sleep(50); // T = 350 

                Assert.That(threadPool.Execute(() => { log.Log("task 6: executed"); }, 100), Is.False); // T = 450

                Thread.Sleep(50);
                log.Log("main: wait started"); // T = 500
                Thread.Sleep(200);
                log.Log("main: wait finished"); // T = 700


                Assert.That(log.GetLog(), Is.EqualTo(new string[]
                                                     {
                                                         "task 1: started",
                                                         "task 2: started",
                                                         "task 1: finished",
                                                         "task 3: started",
                                                         "main: wait started",
                                                         "task 2: finished",
                                                         "task 5: started",
                                                         "task 3: finished",
                                                         "task 5: finished",
                                                         "main: wait finished"
                                                     }));
            }

            Assert.That(threadPool.Stop(1000), Is.True);
        }

        [Test]
        public void TestNoBlockThenBlock()
        {
            BlockingThreadPool threadPool = new BlockingThreadPool(2, 2);
            MessageLog log = new MessageLog();

            for (int i = 0; i < TestIterationsCount; i++)
            {
                log.Clear();

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 1: started"); // T = 0
                                                   Thread.Sleep(100);
                                                   log.Log("task 1: finished"); // T = 100
                                               }, 100), Is.True);

                Thread.Sleep(50); // T = 50

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 2: started"); // T = 50
                                                   Thread.Sleep(200);
                                                   log.Log("task 2: finished"); // T = 250
                                               }, 100), Is.True);

                Thread.Sleep(100); // T = 150

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 3: started"); // T = 150
                                                   Thread.Sleep(250);
                                                   log.Log("task 3: finished"); // T = 400
                                               }, 100), Is.True);

                Thread.Sleep(50); // T = 200

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 4: started"); // T = 250
                                                   Thread.Sleep(100);
                                                   log.Log("task 4: finished"); // T = 350
                                               }, 100), Is.True); // T = 250

                Thread.Sleep(50);
                log.Log("main: wait started"); // T = 300
                Thread.Sleep(150);
                log.Log("main: wait finished"); // T = 450

                Assert.That(log.GetLog(), Is.EqualTo(new string[]
                                                     {
                                                         "task 1: started",
                                                         "task 2: started",
                                                         "task 1: finished",
                                                         "task 3: started",
                                                         "task 2: finished",
                                                         "task 4: started",
                                                         "main: wait started",
                                                         "task 4: finished",
                                                         "task 3: finished",
                                                         "main: wait finished"
                                                     }));
            }

            Assert.That(threadPool.Stop(1000), Is.True);
        }

        [Test]
        public void TestStopIdle()
        {
            BlockingThreadPool threadPool = new BlockingThreadPool(2, 2);
            Thread.Sleep(50);
            Assert.That(threadPool.Stop(50), Is.True);
        }

        [Test]
        public void TestStopBusy()
        {
            MessageLog log = new MessageLog();

            for (int i = 0; i < TestIterationsCount; i++)
            {
                BlockingThreadPool threadPool = new BlockingThreadPool(3, 3);
                log.Clear();

                threadPool.Execute(() =>
                {
                    log.Log("task 1: started"); // T = 0
                    try
                    {
                        // finally block is required to ignore Thread.Interrupt and Thread.Abort
                    }
                    finally
                    {
                        Thread.Sleep(200);
                    }
                    log.Log("task 1: finished"); // T = 200
                }, 100);

                Thread.Sleep(50); // T = 50

                threadPool.Execute(() =>
                {
                    log.Log("task 2: started"); // T = 50
                    try
                    {
                        // finally block is required to ignore Thread.Interrupt and Thread.Abort
                    }
                    finally
                    {
                        Thread.Sleep(250);
                    }
                    log.Log("task 2: finished"); // T = 300
                }, 100);

                Thread.Sleep(50); // T = 50

                threadPool.Execute(() =>
                {
                    log.Log("task 3: started"); // T = 100
                    log.Log(string.Format("task 4 queued: {0}", threadPool.Execute(() => { }, 300))); // T = 150
                    log.Log("task 3: finished"); // T = 150
                }, 100);

                Thread.Sleep(50); // T = 150

                Stopwatch sw = Stopwatch.StartNew();
                Assert.That(threadPool.Stop(100), Is.False); // T = 250
                Assert.That(sw.ElapsedMilliseconds, Is.GreaterThan(75).And.LessThan(125));

                sw.Restart();
                Assert.That(threadPool.Execute(() => { }, 100), Is.False);
                Assert.That(sw.ElapsedMilliseconds, Is.LessThan(25));

                sw.Restart();
                Assert.That(threadPool.Stop(100), Is.True); // T = 300
                Assert.That(sw.ElapsedMilliseconds, Is.GreaterThan(25).And.LessThan(75));

                Assert.That(log.GetLog(), Is.EqualTo(new string[]
                                                 {
                                                     "task 1: started",
                                                     "task 2: started",
                                                     "task 3: started",
                                                     "task 4 queued: False",
                                                     "task 3: finished",
                                                     "task 1: finished",
                                                     "task 2: finished"
                                                 }));
            }
        }

        [Test]
        public void TestPerformance()
        {
            const int poolThreadCount = 1000;
            const int taskDelay = 10;

            BlockingThreadPool threadPool = new BlockingThreadPool(poolThreadCount, poolThreadCount*2);

            object lockObject = new object();

            long executedTasks = 0;
            long rejectedTasks = 0;
            int threadCount = 0;
            int maxThreadCount = 0;

            Action task = () =>
                          {
                              var newThreadCount = Interlocked.Increment(ref threadCount);
                              lock (lockObject)
                              {
                                  if (newThreadCount > maxThreadCount)
                                  {
                                      maxThreadCount = newThreadCount;
                                  }
                              }

                              Thread.Sleep(taskDelay);

                              Interlocked.Increment(ref executedTasks);
                              Interlocked.Decrement(ref threadCount);
                          };

            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 10000)
            {
                for (int i = 0; i < 1000; i++)
                {
                    if (!threadPool.Execute(task, taskDelay + 100))
                    {
                        rejectedTasks++;
                    }
                }
            }

            Assert.That(threadPool.Stop(1000), Is.True);

            Console.WriteLine("executedTasks:\t{0}", executedTasks);
            Console.WriteLine("rejectedTasks:\t{0}", rejectedTasks);
            Console.WriteLine("maxThreadCount:\t{0}", maxThreadCount);

            Assert.That(maxThreadCount, Is.EqualTo(poolThreadCount));
            Assert.That(rejectedTasks, Is.EqualTo(0));
        }

        private class MessageLog
        {
            private readonly object logLock = new StringBuilder();
            private readonly List<string> log = new List<string>();
            private readonly Stopwatch timer = Stopwatch.StartNew();

            public string[] GetLog()
            {
                lock (logLock)
                {
                    return log.ToArray();
                }
            }

            public void Log(string message)
            {
                lock (logLock)
                {
                    log.Add(message);
                    Console.WriteLine("{0,3}: {1}", timer.ElapsedMilliseconds, message);
                }
            }

            public void Clear()
            {
                timer.Restart();
                lock (logLock)
                {
                    log.Clear();
                    Console.WriteLine("=========================================");
                }
            }
        }
    }
}