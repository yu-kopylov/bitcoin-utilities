using System;
using System.Collections.Generic;
using System.Threading;

namespace BitcoinUtilities.Threading
{
    public class ConcurrentEnumerator<T> : IConcurrentEnumerator<T>
    {
        private readonly IReadOnlyList<T> collection;
        private int prevIndex = -1;

        public ConcurrentEnumerator(IReadOnlyList<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection), $"The given {nameof(collection)} is null.");
            }

            this.collection = collection;
        }

        public bool GetNext(out T value)
        {
            int index = Interlocked.Increment(ref prevIndex);
            if (index < collection.Count)
            {
                value = collection[index];
                return true;
            }

            // prevents prevIndex from growing indefinitely
            Interlocked.CompareExchange(ref prevIndex, collection.Count, index);

            value = default(T);
            return false;
        }

        /// <summary>
        /// Concurrently applies the given function for each given resource and this enumerator.
        /// </summary>
        /// <typeparam name="TResource">The type of resource used by the function.</typeparam>
        /// <typeparam name="TResult">The type of result returned by the function.</typeparam>
        /// <param name="resources">The list of resources.</param>
        /// <param name="function">The function to apply.</param>
        /// <exception cref="AggregateException">If one of more function calls throws an exception.</exception>
        /// <returns>A list of results returned by function calls.</returns>
        public List<TResult> ProcessWith<TResource, TResult>(IReadOnlyList<TResource> resources, Func<TResource, IConcurrentEnumerator<T>, TResult> function)
        {
            List<TResult> results = new List<TResult>();
            List<Exception> exceptions = new List<Exception>();

            using (ManualResetEvent completionEvent = new ManualResetEvent(false))
            {
                ProcessingState<TResult> state = new ProcessingState<TResult>(completionEvent, resources.Count, results, exceptions);

                foreach (TResource resource in resources)
                {
                    var workerThread = new WorkerThread<TResource, TResult>(state, resource, this, function);
                    ThreadPool.QueueUserWorkItem(workerThread.Run);
                }

                completionEvent.WaitOne();
            }

            if (exceptions.Count != 0)
            {
                throw new AggregateException("Error in a the worker threads.", exceptions);
            }

            return results;
        }

        private class ProcessingState<TResult>
        {
            private readonly object monitor = new object();
            private readonly ManualResetEvent completionEvent;

            private int activeWorkers;
            private readonly List<TResult> results;
            private readonly List<Exception> exceptions;

            public ProcessingState(ManualResetEvent completionEvent, int activeWorkers, List<TResult> results, List<Exception> exceptions)
            {
                this.completionEvent = completionEvent;
                this.activeWorkers = activeWorkers;
                this.results = results;
                this.exceptions = exceptions;
            }

            public void ReportResult(TResult result)
            {
                lock (monitor)
                {
                    results.Add(result);
                    RemoveWorker();
                }
            }

            public void ReportException(Exception exception)
            {
                lock (monitor)
                {
                    exceptions.Add(exception);
                    RemoveWorker();
                }
            }

            private void RemoveWorker()
            {
                activeWorkers--;
                if (activeWorkers == 0)
                {
                    completionEvent.Set();
                }
            }
        }

        private class WorkerThread<TResource, TResult>
        {
            private readonly ProcessingState<TResult> state;
            private readonly TResource resource;
            private readonly ConcurrentEnumerator<T> enumerator;
            private readonly Func<TResource, IConcurrentEnumerator<T>, TResult> function;

            public WorkerThread(ProcessingState<TResult> state, TResource resource, ConcurrentEnumerator<T> enumerator, Func<TResource, IConcurrentEnumerator<T>, TResult> function)
            {
                this.state = state;
                this.resource = resource;
                this.enumerator = enumerator;
                this.function = function;
            }

            public void Run(object _)
            {
                try
                {
                    TResult result = function(resource, enumerator);
                    state.ReportResult(result);
                }
                catch (Exception e)
                {
                    state.ReportException(e);
                }
            }
        }
    }
}