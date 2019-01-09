using System;
using System.Collections.Generic;
using System.Threading;
using NLog;

namespace BitcoinUtilities.Threading
{
    /// <summary>
    /// <p>Manages starting and stoping of services that consume events, and distributes events between them.</p>
    /// <p>Uses different threads for concurrent processing of events.</p>
    /// <p>Each service can assume that it is called from one thread only, and that events are passed to it in the same order in which they were raised.</p>
    /// </summary>
    public class EventServiceController : IDisposable
    {
        private readonly object monitor = new object();
        private readonly List<ServiceThread> services = new List<ServiceThread>();

        // todo: do we need thread-safety for service registration?
        private volatile bool started = false;

        public void Dispose()
        {
            Stop();

            // todo: avoid excessive locks
            lock (monitor)
            {
                foreach (var service in services)
                {
                    service.Dispose();
                }
            }
        }

        public void Start()
        {
            started = true;
            lock (monitor)
            {
                foreach (var service in services)
                {
                    service.Start();
                }
            }
        }

        public void Stop()
        {
            started = false;
            lock (monitor)
            {
                foreach (var service in services)
                {
                    service.Stop();
                }
            }

            lock (monitor)
            {
                foreach (var service in services)
                {
                    service.Join();
                }
            }
        }

        public void AddService(IEventHandlingService service)
        {
            var serviceThread = new ServiceThread(service);
            lock (monitor)
            {
                services.Add(serviceThread);
            }

            if (started)
            {
                // todo: add test
                serviceThread.Start();
            }
        }

        public T GetService<T>()
        {
            lock (monitor)
            {
                foreach (var service in services)
                {
                    if (service.Service is T innerService)
                    {
                        return innerService;
                    }
                }
            }

            return default(T);
        }

        public void Raise(object evt)
        {
            lock (monitor)
            {
                foreach (var service in services)
                {
                    if (service.Expects(evt))
                    {
                        service.Queue(evt);
                    }
                }
            }
        }

        private class ServiceThread : IDisposable
        {
            private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

            private readonly object monitor = new object();
            private readonly AutoResetEvent stateChangedEvent = new AutoResetEvent(false);
            private readonly Queue<object> events = new Queue<object>();

            private Thread thread;
            private volatile bool stopped = false;

            public ServiceThread(IEventHandlingService service)
            {
                Service = service;
            }

            public void Dispose()
            {
                Stop();
                stateChangedEvent.Dispose();
            }

            internal IEventHandlingService Service { get; }

            public void Start()
            {
                thread = new Thread(ServiceLoop);
                thread.Start();
            }

            public void Stop()
            {
                stopped = true;

                lock (monitor)
                {
                    events.Clear();
                }

                stateChangedEvent.Set();
            }

            public void Join()
            {
                thread.Join();
            }

            public void Queue(object evt)
            {
                const int maxQueueSize = 8192;
                lock (monitor)
                {
                    // todo: add per endpoint restriction insteads
                    if (events.Count >= maxQueueSize)
                    {
                        throw new InvalidOperationException(
                            $"Cannot queue event '{evt}' for service '{Service}', because queue already has {maxQueueSize} events."
                        );
                    }

                    events.Enqueue(evt);
                }

                stateChangedEvent.Set();
            }

            public bool Expects(object @event)
            {
                return Service.GetHandler(@event) != null;
            }

            private void ServiceLoop()
            {
                try
                {
                    Service.OnStart();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Error during startup of service '{Service}'.");
                    throw;
                }

                while (!stopped)
                {
                    object evt = DequeEvent();
                    if (evt != null)
                    {
                        try
                        {
                            HandleEvent(evt);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, $"Service '{Service}' failed to handle event '{evt}'.");
                            break;
                        }
                    }
                    else
                    {
                        stateChangedEvent.WaitOne();
                    }
                }

                try
                {
                    Service.OnTearDown();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Error during tear-down of service '{Service}'.");
                    throw;
                }
            }

            private object DequeEvent()
            {
                lock (monitor)
                {
                    if (events.Count == 0)
                    {
                        return null;
                    }

                    return events.Dequeue();
                }
            }

            private void HandleEvent(object evt)
            {
                if (stopped)
                {
                    return;
                }

                var handler = Service.GetHandler(evt);
                if (handler == null)
                {
                    throw new InvalidOperationException($"Handler not found for event '{evt}' in service '{Service}'.");
                }

                handler(evt);
            }
        }
    }
}