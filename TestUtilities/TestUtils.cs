using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace TestUtilities
{
    public class TestUtils
    {
        /// <summary>
        /// Creates folder for tests which name is based on type of the test and a given relative path.
        /// <para/>
        /// Removes files matching given patterns from the test folder.
        /// </summary>
        /// <param name="testType">The type of the test.</param>
        /// <param name="relativePath">The relative path.</param>
        /// <param name="patterns">The patterns of files that should be deleted.</param>
        /// <returns>A full path to the test folder.</returns>
        public static string PrepareTestFolder(Type testType, string relativePath = "Test", params string[] patterns)
        {
            string testFolder = Path.GetFullPath(Path.Combine("tmp-test", testType.Name, relativePath));

            Console.WriteLine($"Removing files in the test folder: {testFolder}");

            Directory.CreateDirectory(testFolder);
            foreach (string pattern in patterns)
            {
                foreach (string filename in Directory.GetFiles(testFolder, pattern, SearchOption.TopDirectoryOnly))
                {
                    Console.WriteLine($"Removing: {filename}");
                    File.Delete(filename);
                }
            }

            return testFolder;
        }

        /// <summary>
        /// Creates folder for tests which name is based on type of the test and test method name.
        /// <para/>
        /// Removes files matching given patterns from the test folder.
        /// </summary>
        /// <param name="patterns">The patterns of files that should be deleted.</param>
        /// <returns>A full path to the test folder.</returns>
        public static string PrepareTestFolder(params string[] patterns)
        {
            var method = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod();
            return PrepareTestFolder(method.ReflectedType, method.Name, patterns);
        }

        /// <summary>
        /// Executes the given check delegate multiple times in multiple threads. Waits for all threads to become ready before starting check iterations.
        /// </summary>
        /// <param name="threadCount">The number of threads.</param>
        /// <param name="iterationCount">The number of checks to perform in each thread.</param>
        /// <param name="check">The check delegate with thread number and iteration number as parameters. Should return true if check is successful.</param>
        /// <exception cref="Exception">If not all of the checks passed; or if one of the threads thrown an exception.</exception>
        public static void TestConcurrency(int threadCount, int iterationCount, Func<int, int, bool> check)
        {
            Thread[] threads = new Thread[threadCount];
            ConcurrentBag<Exception> exceptions = new ConcurrentBag<Exception>();
            long readyThreads = 0;
            long passedChecks = 0;
            using (ManualResetEvent startSignal = new ManualResetEvent(false))
            {
                for (int t = 0; t < threadCount; t++)
                {
                    int threadNum = t;
                    var thread = new Thread(() =>
                    {
                        try
                        {
                            Interlocked.Increment(ref readyThreads);
                            startSignal.WaitOne();
                            for (int i = 0; i < iterationCount; i++)
                            {
                                if (check(threadNum, i))
                                {
                                    Interlocked.Increment(ref passedChecks);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            exceptions.Add(e);
                        }
                    });
                    thread.Name = "TestConcurrency";
                    thread.IsBackground = true;
                    threads[t] = thread;
                    thread.Start();
                }

                while (Interlocked.Read(ref readyThreads) < threadCount)
                {
                    Thread.Sleep(10);
                }

                startSignal.Set();

                foreach (var thread in threads)
                {
                    thread.Join();
                }
            }

            if (exceptions.TryTake(out var ex))
            {
                throw new Exception("Exception in one of the threads.", ex);
            }

            long expectedPassedChecks = threadCount * (long) iterationCount;
            if (expectedPassedChecks != passedChecks)
            {
                throw new Exception($"Passed {passedChecks} of {expectedPassedChecks} checks.");
            }
        }
    }
}