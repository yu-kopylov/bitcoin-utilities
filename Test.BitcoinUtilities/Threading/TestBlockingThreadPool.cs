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
        [Test]
        public void TestNoBlock()
        {
            MessageLog log = new MessageLog();

            BlockingThreadPool threadPool = new BlockingThreadPool(2, 2);

            for (int i = 0; i < 2; i++)
            {
                log.Clear();

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 1: started"); // T = 0
                                                   Thread.Sleep(100);
                                                   log.Log("task 1: finished"); // T = 100
                                               }, 100), Is.True);

                Thread.Sleep(10);
                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 2: started"); // T = 10
                                                   Thread.Sleep(100);
                                                   log.Log("task 2: finished"); // T = 110
                                               }, 100), Is.True);

                Thread.Sleep(10);
                log.Log("main: wait started"); // T = 20
                Thread.Sleep(100);
                log.Log("main: wait finished"); // T = 120

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
        }

        [Test]
        public void TestWithBlock()
        {
            MessageLog log = new MessageLog();

            BlockingThreadPool threadPool = new BlockingThreadPool(2, 2);

            for (int i = 0; i < 2; i++)
            {
                log.Clear();

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 1: started"); // T = 0
                                                   Thread.Sleep(100);
                                                   log.Log("task 1: finished"); // T = 100
                                               }, 100), Is.True);

                Thread.Sleep(10);
                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 2: started"); // T = 10
                                                   Thread.Sleep(200);
                                                   log.Log("task 2: finished"); // T = 210
                                               }, 100), Is.True);

                Thread.Sleep(10);
                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 3: started");
                                                   Thread.Sleep(100);
                                                   log.Log("task 3: finished");
                                               }, 10), Is.False);

                Thread.Sleep(10);
                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 4: started"); // T = 100
                                                   Thread.Sleep(100);
                                                   log.Log("task 4: finished"); // T = 200
                                               }, 100), Is.True);

                Thread.Sleep(10);
                log.Log("main: wait started"); // T = 110
                Thread.Sleep(200);
                log.Log("main: wait finished"); // T = 310

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
        }

        [Test]
        public void TestQueuedTasks()
        {
            BlockingThreadPool threadPool = new BlockingThreadPool(2, 3);
            MessageLog log = new MessageLog();

            for (int i = 0; i < 2; i++)
            {
                log.Clear();

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 1: started"); // T = 0
                                                   Thread.Sleep(100);
                                                   log.Log("task 1: finished"); // T = 100
                                               }, 10), Is.True);

                Thread.Sleep(10); // T = 10 

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 2: started"); // T = 10
                                                   Thread.Sleep(200);
                                                   log.Log("task 2: finished"); // T = 210
                                               }, 10), Is.True);

                Thread.Sleep(10); // T = 20 

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 3: started"); // T = 100
                                                   Thread.Sleep(200);
                                                   log.Log("task 3: finished"); // T = 300
                                               }, 10), Is.True);

                Thread.Sleep(10); // T = 30 

                Assert.That(threadPool.Execute(() => { log.Log("task 4: executed"); }, 10), Is.False); //T = 40

                Thread.Sleep(100); // T = 140

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 5: started"); // T = 210
                                                   Thread.Sleep(100);
                                                   log.Log("task 5: finished"); // T = 310
                                               }, 10), Is.True);

                Thread.Sleep(10); // T = 150 

                Assert.That(threadPool.Execute(() => { log.Log("task 6: executed"); }, 10), Is.False); //T = 160

                Thread.Sleep(10);
                log.Log("main: wait started"); // T = 170
                Thread.Sleep(200);
                log.Log("main: wait finished"); // T = 370


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
        }

        [Test]
        public void TestStop()
        {
            BlockingThreadPool threadPool = new BlockingThreadPool(2, 2);
            threadPool.Stop();
        }

        [Test]
        public void TestNoBlockThenBlock()
        {
            MessageLog log = new MessageLog();

            BlockingThreadPool threadPool = new BlockingThreadPool(2, 2);

            for (int i = 0; i < 2; i++)
            {
                log.Clear();

                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 1: started"); // T = 0
                                                   Thread.Sleep(100);
                                                   log.Log("task 1: finished"); // T = 100
                                               }, 100), Is.True);

                Thread.Sleep(10);
                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 2: started"); // T = 10
                                                   Thread.Sleep(200);
                                                   log.Log("task 2: finished"); // T = 210
                                               }, 100), Is.True);

                Thread.Sleep(110);
                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 3: started"); // T = 120
                                                   Thread.Sleep(200);
                                                   log.Log("task 3: finished"); // T = 320
                                               }, 10), Is.True);

                Thread.Sleep(10);
                Assert.That(threadPool.Execute(() =>
                                               {
                                                   log.Log("task 4: started"); // T = 210
                                                   Thread.Sleep(100);
                                                   log.Log("task 4: finished"); // T = 310
                                               }, 100), Is.True);

                Thread.Sleep(10);
                log.Log("main: wait started"); // T = 220
                Thread.Sleep(200);
                log.Log("main: wait finished"); // T = 420

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
        }

        [Test]
        public void TestPerformance()
        {
            GC.Collect();

            const int poolThreadCount = 100;
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
            while (sw.ElapsedMilliseconds < 1000)
            {
                for (int i = 0; i < 1000; i++)
                {
                    if (!threadPool.Execute(task, taskDelay + 20))
                    {
                        rejectedTasks++;
                    }
                }
            }

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