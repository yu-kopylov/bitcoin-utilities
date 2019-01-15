using System;
using System.Collections.Generic;
using System.Threading;
using BitcoinUtilities.P2P;
using NLog;

namespace BitcoinUtilities.Threading
{
    /// <summary>
    /// <p>Manages starting and stoping of services that consume events, and distributes events between them.</p>
    /// <p>Uses different threads for concurrent processing of events.</p>
    /// <p>Each service can assume that it is called from one thread only, and that events are passed to it in the same order in which they were raised.</p>
    /// </summary>
    public class EventServiceController : IEventDispatcher, IDisposable
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

            List<ServiceThread> stoppedThreads;
            lock (monitor)
            {
                stoppedThreads = new List<ServiceThread>(services);
                foreach (var service in stoppedThreads)
                {
                    service.Stop();
                }
            }

            foreach (var service in stoppedThreads)
            {
                service.Join();
            }
        }

        public void AddService(IEventHandlingService service)
        {
            var serviceThread = new ServiceThread(service);
            lock (monitor)
            {
                services.Add(serviceThread);
                if (started)
                {
                    // todo: add test
                    serviceThread.Start();
                }
            }
        }

        public void RemoveService(IEventHandlingService service)
        {
            ServiceThread serviceThread;
            lock (monitor)
            {
                // todo: use dictionary
                int index = services.FindIndex(s => s.Service == service);
                if (index < 0)
                {
                    throw new InvalidOperationException($"{nameof(EventServiceController)} does not have this service.");
                }

                serviceThread = services[index];
                services.RemoveAt(index);
            }

            // todo: use list of services as parameter to stop all of them before a call to serviceThread.Join
            serviceThread.Stop();
            serviceThread.Join();
            serviceThread.Dispose();
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
                thread.Name = $"ServiceThread<{Service.GetType().Name}>";
                thread.IsBackground = true;
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
                thread?.Join();
            }

            public void Queue(object evt)
            {
                const int maxQueueSize = 16384;
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
                    try
                    {
                        Service.OnStart();
                    }
                    catch (Exception ex)
                    {
                        LogServiceException(ex, null);
                        // if service crashed, its state may be corrupted
                        stopped = true;
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
                                LogServiceException(ex, evt);
                                // if service crashed, its state may be corrupted
                                stopped = true;
                            }
                        }
                        else
                        {
                            stateChangedEvent.WaitOne();
                        }
                    }

                    try
                    {
                        // todo: should OnTearDown be called for crashed services? such services can have corrupted state
                        Service.OnTearDown();
                    }
                    catch (Exception ex)
                    {
                        LogServiceException(ex, null);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Unhandled exception in {Service} thread.");
                }
                finally
                {
                    logger.Debug($"{Service} thread has stopped.");
                    // todo: remove service from controller or properly stop it to avoid queuing events to a queue without handler
                }
            }

            private void LogServiceException(Exception ex, object evt)
            {
                bool ignorableException = ex is BitcoinNetworkException;

                if (ignorableException && logger.IsTraceEnabled)
                {
                    logger.Trace(ex, FormatServiceError(evt));
                }

                if (!ignorableException && logger.IsErrorEnabled)
                {
                    logger.Error(ex, FormatServiceError(evt));
                }
            }

            private string FormatServiceError(object evt)
            {
                string serviceName = Service.GetType().Name;
                if (evt == null)
                {
                    return $"Failure in {serviceName}.";
                }

                return $"{serviceName} failed to handle {evt.GetType().Name}.";
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