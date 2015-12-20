using System;
using System.Collections.Generic;
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
                throw new ArgumentException("Parameter threadCount should be greater than zero.", "threadCount");
            }
            if (taskCountLimit < threadCount)
            {
                throw new ArgumentException("Parameter taskCountLimit should be greater or equal to threadCount.", "taskCountLimit");
            }

            taskQueue = new Queue<Action>(taskCountLimit);
            enqueSemaphore = new Semaphore(taskCountLimit, taskCountLimit);
            dequeSemaphore = new Semaphore(0, taskCountLimit);
            running = true;
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
            return true;
        }

        /// <summary>
        /// Signals pool to stop.
        /// </summary>
        public void Stop()
        {
            //todo: shutdown completely using Interrupt and Abort
            //todo: write better tests
            running = false;
            try
            {
                enqueSemaphore.Release();
            }
            catch (SemaphoreFullException)
            {
                //semaphore was completely released already
            }
            try
            {
                dequeSemaphore.Release();
            }
            catch (SemaphoreFullException)
            {
                //semaphore was completely released already
            }
        }

        private void WorkerThreadLoop()
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
            }
        }

        private void EnqueueTask(Action task)
        {
            lock (taskQueueLock)
            {
                taskQueue.Enqueue(task);
                dequeSemaphore.Release();
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
                // error handling is not a responsibility of the worker thread
                logger.Error(e, "Task in a BlockingThreadPool failed with an exception.");
            }
            finally
            {
                enqueSemaphore.Release();
                Thread.Yield();
            }
        }
    }
}