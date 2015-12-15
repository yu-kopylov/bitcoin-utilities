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

            BlockingThreadPool threadPool = new BlockingThreadPool(2);

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

            BlockingThreadPool threadPool = new BlockingThreadPool(2);

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
        public void TestNoBlockThenBlock()
        {
            MessageLog log = new MessageLog();

            BlockingThreadPool threadPool = new BlockingThreadPool(2);

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
            const int poolThreadCount = 100;
            BlockingThreadPool threadPool = new BlockingThreadPool(poolThreadCount);

            object lockObject = new object();

            long executedTasks = 0;
            long increments = 0;
            int threadCount = 0;
            int maxThreadCount = 0;

            Stopwatch sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < 1000)
            {
                for (int i = 0; i < 1000; i++)
                {
                    threadPool.Execute(() =>
                                       {
                                           int newCount = Interlocked.Increment(ref threadCount);
                                           lock (lockObject)
                                           {
                                               if (newCount > maxThreadCount)
                                               {
                                                   maxThreadCount = newCount;
                                               }
                                           }
                                           Interlocked.CompareExchange(ref maxThreadCount, newCount, newCount);

                                           Interlocked.Increment(ref executedTasks);
                                           Thread.Sleep(10);
                                           for (int j = 0; j < 10; j++)
                                           {
                                               Interlocked.Increment(ref increments);
                                           }
                                           Interlocked.Decrement(ref threadCount);
                                       }, 10);
                }
            }

            Console.WriteLine("executedTasks:\t{0}", executedTasks);
            Console.WriteLine("increments:\t{0}", increments);
            Console.WriteLine("maxThreadCount:\t{0}", maxThreadCount);

            Assert.That(maxThreadCount, Is.EqualTo(poolThreadCount));
        }

        private class MessageLog
        {
            private readonly object logLock = new StringBuilder();
            private readonly List<string> log = new List<string>();
            private readonly Stopwatch timer = new Stopwatch();

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