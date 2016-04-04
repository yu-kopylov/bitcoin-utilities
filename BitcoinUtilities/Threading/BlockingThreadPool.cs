using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NLog;

namespace BitcoinUtilities.Threading
{
    /// <summary>
    /// A thread pool with a fixed number of threads and a limited number of tasks that can be accepted simultaneously.
    /// <para/>
    /// If there is no space in the pool for a new task then a caller of <see cref="Execute"/> method will be delayed.
    /// </summary>
    public class BlockingThreadPool
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Thread[] threads;
        private volatile bool running;
        private volatile bool disposed;

        private readonly object managementLock = new object();

        private readonly object taskQueueLock = new object();
        private readonly Queue<Action> taskQueue;

        private readonly Semaphore enqueSemaphore;
        private readonly Semaphore dequeSemaphore;

        /// <summary>
        /// Initializes the a new instance of the pool. 
        /// </summary>
        /// <param name="threadCount">The number of threads in the pool.</param>
        /// <param name="taskCountLimit">The maximal number of tasks that can be accepted simultaneously.</param>
        public BlockingThreadPool(int threadCount, int taskCountLimit)
        {
            if (threadCount <= 0)
            {
                throw new ArgumentException($"Parameter {nameof(threadCount)} should be greater than zero.", nameof(threadCount));
            }
            if (taskCountLimit < threadCount)
            {
                throw new ArgumentException($"Parameter {nameof(taskCountLimit)} should be greater or equal to {nameof(threadCount)}.", nameof(taskCountLimit));
            }

            taskQueue = new Queue<Action>(taskCountLimit);
            enqueSemaphore = new Semaphore(taskCountLimit, taskCountLimit + 1);
            dequeSemaphore = new Semaphore(0, taskCountLimit + 1);
            running = true;
            disposed = false;
            threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                Thread thread = new Thread(WorkerThreadLoop);
                threads[i] = thread;

                thread.Name = "Thread#" + i;
                thread.IsBackground = true;

                thread.Start();
            }
        }

        /// <summary>
        /// Shedules the task for execution.
        /// <para/>
        /// Blocks until there is a space in the pool.
        /// </summary>
        /// <param name="task">The task to execute.</param>
        /// <param name="timeout">Time in milliseconds to wait for an available space, or -1 to wait indefinitely.</param>
        /// <returns>true if the task was scheduled for execution; otherwise, false.</returns>
        public bool Execute(Action task, int timeout)
        {
            //todo: add tests for infinite wait(-1) or remove this option
            if (!enqueSemaphore.WaitOne(timeout))
            {
                return false;
            }
            if (!running)
            {
                //Signal other threads to stop waiting.
                enqueSemaphore.Release();
                return false;
            }

            EnqueueTask(task);
            dequeSemaphore.Release();
            
            return true;
        }

        /// <summary>
        /// Signals pool to stop.
        /// </summary>
        /// <param name="timeout">Time in milliseconds to wait for worker threads to stop.</param>
        /// <returns>true if all worker threads was stopped; otherwise, false.</returns>
        public bool Stop(int timeout)
        {
            //todo: allow infinite timeout?
            //todo: shutdown completely using Interrupt and Abort ?

            lock (managementLock)
            {
                if (running)
                {
                    running = false;
                    enqueSemaphore.Release();
                    dequeSemaphore.Release();
                }
            }

            Stopwatch sw = Stopwatch.StartNew();
            foreach (Thread thread in threads)
            {
                long remainingTimeout = timeout - sw.ElapsedMilliseconds;
                if (remainingTimeout < 0)
                {
                    remainingTimeout = 0;
                }
                if (!thread.Join((int) remainingTimeout))
                {
                    return false;
                }
            }

            lock (managementLock)
            {
                if (!disposed)
                {
                    enqueSemaphore.Dispose();
                    dequeSemaphore.Dispose();
                    disposed = true;
                }
            }

            return true;
        }

        private void WorkerThreadLoop()
        {
            try
            {
                while (running)
                {
                    dequeSemaphore.WaitOne(-1);
                    if (!running)
                    {
                        //Signal other threads to stop waiting.
                        dequeSemaphore.Release();
                        return;
                    }

                    Action task = DequeueTask();
                    ExecuteTask(task);

                    enqueSemaphore.Release();
                    Thread.Yield();
                }
            }
            catch (Exception e)
            {
                //todo: test this
                logger.Fatal(e, "An unexpected error occured in the worker thread. The worker thread stopped.");
            }
        }

        private void EnqueueTask(Action task)
        {
            lock (taskQueueLock)
            {
                taskQueue.Enqueue(task);
            }
        }

        private Action DequeueTask()
        {
            lock (taskQueueLock)
            {
                return taskQueue.Dequeue();
            }
        }

        private void ExecuteTask(Action task)
        {
            try
            {
                task();
            }
            catch (Exception e)
            {
                logger.Error(e, "Task in a BlockingThreadPool failed with an exception.");
            }
        }
    }
}