using System;
using System.Collections.Generic;
using System.Globalization;
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

            foreach (var service in services)
            {
                service.Dispose();
            }
        }

        public void Start()
        {
            started = true;
            foreach (var service in services)
            {
                service.Start();
            }
        }

        public void Stop()
        {
            started = false;
            foreach (var service in services)
            {
                service.Stop();
            }

            foreach (var service in services)
            {
                service.Join();
            }
        }

        public void AddService(IEventHandlingService service)
        {
            var serviceThread = new ServiceThread(service);
            services.Add(serviceThread);
            if (started)
            {
                // todo: add test
                serviceThread.Start();
            }
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

            private readonly IEventHandlingService service;

            private Thread thread;
            private volatile bool stopped = false;

            public ServiceThread(IEventHandlingService service)
            {
                this.service = service;
            }

            public void Dispose()
            {
                Stop();
                stateChangedEvent.Dispose();
            }

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
                const int maxQueueSize = 16;
                lock (monitor)
                {
                    // todo: add per endpoint restriction insteads
                    if (events.Count >= maxQueueSize)
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                            "Cannot queue event '{0}' for service '{1}', because queue already has {2} events.",
                            evt, service, maxQueueSize
                        ));
                    }

                    events.Enqueue(evt);
                }

                stateChangedEvent.Set();
            }

            public bool Expects(object @event)
            {
                return service.GetHandler(@event) != null;
            }

            private void ServiceLoop()
            {
                try
                {
                    service.OnStart();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, CultureInfo.InvariantCulture, "Error during startup of service '{0}'.", service);
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
                            logger.Error(ex, CultureInfo.InvariantCulture, "Service '{0}' failed to handle event '{1}'.", service, evt);
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
                    service.OnTearDown();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, CultureInfo.InvariantCulture, "Error during tear-down of service '{0}'.", service);
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

                var handler = service.GetHandler(evt);
                if (handler == null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Handler not found for event '{0}' in service '{1}'.", evt, service));
                }

                handler(evt);
            }
        }
    }
}