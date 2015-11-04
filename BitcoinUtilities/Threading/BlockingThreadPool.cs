using System;
using System.Collections.Generic;
using System.Threading;

namespace BitcoinUtilities.Threading
{
    /// <summary>
    /// A thread pool with fixed number of threads.
    /// <para/>
    /// If all threads are busy a caller of <see cref="Execute"/> method will be delayed.
    /// </summary>
    public class BlockingThreadPool
    {
        private readonly Thread[] threads;
        private volatile bool running;

        private readonly object taskQueueLock = new object();
        private readonly Queue<Action> taskQueue;
        private int availableThreads;

        private readonly AutoResetEvent taskAvailableEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent threadAvailableEvent = new AutoResetEvent(false);

        /// <summary>
        /// Initializes the a new instance of the pool. 
        /// </summary>
        /// <param name="threadCount">The number of threads in the pool.</param>
        public BlockingThreadPool(int threadCount)
        {
            taskQueue = new Queue<Action>(threadCount);
            availableThreads = threadCount;
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
        /// <param name="timeout">Time in milliseconds to wait for an available thread.</param>
        /// <returns>true if the task was scheduled for execution; otherwise, false.</returns>
        public bool Execute(Action task, int timeout)
        {
            if (EnqueueTask(task))
            {
                return true;
            }
            if (!threadAvailableEvent.WaitOne(timeout))
            {
                return false;
            }
            if (!running)
            {
                //Signal other threads to stop waiting.
                threadAvailableEvent.Set();
                return false;
            }
            return EnqueueTask(task);
        }

        /// <summary>
        /// Signals pool to stop.
        /// </summary>
        public void Stop()
        {
            //todo: shutdown completely using Interrupt and Abort
            //todo: write tests
            running = false;
            taskAvailableEvent.Set();
            threadAvailableEvent.Set();
        }

        private void WorkerThreadLoop()
        {
            while (running)
            {
                Action task;
                if (TryDequeueTask(out task))
                {
                    ExecuteTask(task);
                    ReleaseThread();
                }
                else
                {
                    if (taskAvailableEvent.WaitOne(60000))
                    {
                        if (!running)
                        {
                            //Signal other threads to stop waiting.
                            taskAvailableEvent.Set();
                        }
                    }
                }
            }
        }

        private bool EnqueueTask(Action task)
        {
            lock (taskQueueLock)
            {
                if (taskQueue.Count < availableThreads)
                {
                    taskQueue.Enqueue(task);
                    taskAvailableEvent.Set();
                    return true;
                }
            }
            return false;
        }

        private bool TryDequeueTask(out Action task)
        {
            lock (taskQueueLock)
            {
                if (taskQueue.Count == 0)
                {
                    task = null;
                    return false;
                }
                availableThreads--;
                task = taskQueue.Dequeue();
                return true;
            }
        }

        private void ReleaseThread()
        {
            lock (taskQueueLock)
            {
                availableThreads++;
                threadAvailableEvent.Set();
            }
        }

        private void ExecuteTask(Action task)
        {
            try
            {
                task();
            }
            catch (Exception)
            {
                // error handling is not a responsibility of the worker thread
            }
        }
    }
}