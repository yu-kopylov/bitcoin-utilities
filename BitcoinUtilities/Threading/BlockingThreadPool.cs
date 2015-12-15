using System;
using System.Collections.Generic;
using System.Threading;
using NLog;

namespace BitcoinUtilities.Threading
{
    /// <summary>
    /// A thread pool with fixed number of threads.
    /// <para/>
    /// If all threads are busy a caller of <see cref="Execute"/> method will be delayed.
    /// </summary>
    public class BlockingThreadPool
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Thread[] threads;
        private volatile bool running;

        private readonly object taskQueueLock = new object();
        private readonly Queue<Action> taskQueue;

        private readonly Semaphore taskQueueSemaphore;
        private readonly Semaphore workerSemaphore;

        /// <summary>
        /// Initializes the a new instance of the pool. 
        /// </summary>
        /// <param name="threadCount">The number of threads in the pool.</param>
        public BlockingThreadPool(int threadCount)
        {
            //todo: separate threadCount and queueSize
            taskQueue = new Queue<Action>(threadCount);
            taskQueueSemaphore = new Semaphore(threadCount, threadCount);
            workerSemaphore = new Semaphore(0, threadCount);
            running = true;
            threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                Thread thread = new Thread(WorkerThreadLoop);
                threads[i] = thread;

                thread.Name = "Thread#" + i;
                thread.IsBackground = true; // todo: should it be background?

                thread.Start();
            }
        }

        /// <summary>
        /// Shedules the task for execution.
        /// <para/>
        /// Blocks until there is an available thead.
        /// </summary>
        /// <param name="task">The task to execute.</param>
        /// <param name="timeout">Time in milliseconds to wait for an available thread, or -1 to wait indefinitely.</param>
        /// <returns>true if the task was scheduled for execution; otherwise, false.</returns>
        public bool Execute(Action task, int timeout)
        {
            //todo: add tests for infinite wait(-1) or remove this option
            if (!taskQueueSemaphore.WaitOne(timeout))
            {
                return false;
            }
            if (!running)
            {
                //Signal other threads to stop waiting.
                taskQueueSemaphore.Release();
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
            //todo: write tests for Stop method
            running = false;
            //todo: SemaphoreFullException is possible here
            taskQueueSemaphore.Release();
            workerSemaphore.Release();
        }

        private void WorkerThreadLoop()
        {
            while (running)
            {
                workerSemaphore.WaitOne(-1);
                if (!running)
                {
                    //Signal other threads to stop waiting.
                    workerSemaphore.Release();
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
                workerSemaphore.Release();
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
                taskQueueSemaphore.Release();
                Thread.Yield();
            }
        }
    }
}