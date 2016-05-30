using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;

namespace BitcoinUtilities.GUI.Models
{
    /// <summary>
    /// This class serves as a bridge between sources that produce events frequently and possibly slow event handlers.
    /// <para/>
    /// A handler for an event type would be called exactly once for any series of events of same type that happen before handler is called.
    /// <para/>
    /// All handlers are called within a single thread that is controled by this class.
    /// <para/>
    /// All public methods are thread-safe.
    /// </summary>
    public class EventManager : IDisposable
    {
        public static readonly int TreadShutdownPeriod = 15000;

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly object firedEventsLock = new object();
        private readonly object listenersLock = new object();
        private readonly object threadLock = new object();

        private readonly HashSet<string> firedEvents = new HashSet<string>();
        private readonly Dictionary<string, List<EventListener>> eventListeners = new Dictionary<string, List<EventListener>>();

        private Thread thread;
        private bool started;
        private readonly AutoResetEvent workerWaitEvent = new AutoResetEvent(false);

        /// <summary>
        /// Starts the event manager.
        /// </summary>
        public void Start()
        {
            lock (threadLock)
            {
                if (started)
                {
                    logger.Warn($"{nameof(EventManager)} was already started.");
                    return;
                }

                started = true;

                thread = new Thread(WorkerLoop);
                thread.IsBackground = true;
                thread.Name = $"{nameof(EventManager)}@{GetHashCode()}";
                thread.Start();
            }
        }

        /// <summary>
        /// Stops the event manager.
        /// <para/>
        /// If executing thread is busy, then waits for it to stop but no longer then <see cref="TreadShutdownPeriod"/> milliseconds.
        /// </summary>
        public void Stop()
        {
            Thread threadToWait = null;
            lock (threadLock)
            {
                if (started)
                {
                    started = false;
                    threadToWait = thread;
                }
            }
            if (threadToWait != null)
            {
                workerWaitEvent.Set();
                threadToWait.Join(TreadShutdownPeriod);
            }
        }

        /// <summary>
        /// Stops the event manager.
        /// <para/>
        /// If executing thread is busy, then waits for it to stop but no longer then <see cref="TreadShutdownPeriod"/> milliseconds.
        /// </summary>
        public void Dispose()
        {
            Stop();

            workerWaitEvent.Dispose();
        }

        /// <summary>
        /// Informs the EventManager about occured event.
        /// <para/>
        /// This method is fast. It never waits any events handlers to accept or process the event.
        /// </summary>
        public void Notify(string eventType)
        {
            lock (firedEventsLock)
            {
                firedEvents.Add(eventType);
            }
            workerWaitEvent.Set();
        }

        /// <summary>
        /// Adds a handler for a given event type.
        /// </summary>
        /// <param name="eventType">The event type for which handler would be registered.</param>
        /// <param name="action">The action that should be performed when the event happens.</param>
        public void Watch(string eventType, Action action)
        {
            lock (listenersLock)
            {
                List<EventListener> listeners;
                if (!eventListeners.TryGetValue(eventType, out listeners))
                {
                    listeners = new List<EventListener>();
                    eventListeners.Add(eventType, listeners);
                }
                if (listeners.Any(l => l.IsActionEquals(action)))
                {
                    //todo: throw error instead?
                    logger.Warn($"The same action was already registered for '{eventType}' event.");
                }
                else
                {
                    EventListener eventListener = new EventListener(action);
                    listeners.Add(eventListener);
                }
            }
        }

        /// <summary>
        /// Removes a handler for a given event type.
        /// </summary>
        /// <param name="eventType">The event type for which handler would be removed.</param>
        /// <param name="action">The action that was previously registered for the given event type.</param>
        public void Unwatch(string eventType, Action action)
        {
            lock (listenersLock)
            {
                List<EventListener> listeners;
                if (!eventListeners.TryGetValue(eventType, out listeners))
                {
                    //todo: throw error instead?
                    logger.Warn($"No listenters are registered for '{eventType}' event.");
                    return;
                }
                EventListener listener = listeners.FirstOrDefault(l => l.IsActionEquals(action));
                if (listener == null)
                {
                    //todo: throw error instead?
                    logger.Warn($"This action was not registered for '{eventType}' event.");
                }
                else
                {
                    listeners.Remove(listener);
                }
            }
        }

        private void WorkerLoop()
        {
            while (IsStarted())
            {
                if (workerWaitEvent.WaitOne(TimeSpan.FromMilliseconds(100)))
                {
                    OnTick();
                }
            }
        }

        private bool IsStarted()
        {
            lock (threadLock)
            {
                return started;
            }
        }

        private void OnTick()
        {
            List<string> localFiredEvents;

            lock (firedEventsLock)
            {
                localFiredEvents = firedEvents.ToList();
                firedEvents.Clear();
            }

            foreach (string firedEvent in localFiredEvents)
            {
                if (!IsStarted())
                {
                    return;
                }
                ProcessEvent(firedEvent);
            }
        }

        private void ProcessEvent(string eventType)
        {
            var localListeners = FetchListeners(eventType);

            if (localListeners == null)
            {
                return;
            }

            foreach (EventListener listener in localListeners)
            {
                Action action = listener.Action;
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    logger.Error(e, $"Listener failed to process '{eventType}' event.");
                }
            }
        }

        private List<EventListener> FetchListeners(string eventType)
        {
            List<EventListener> localListeners = null;

            lock (listenersLock)
            {
                List<EventListener> listeners;
                if (eventListeners.TryGetValue(eventType, out listeners))
                {
                    localListeners = new List<EventListener>(listeners);
                }
            }

            return localListeners;
        }

        private class EventListener
        {
            private readonly Action action;

            public EventListener(Action action)
            {
                this.action = action;
            }

            public Action Action
            {
                get { return action; }
            }

            public bool IsActionEquals(Action otherAction)
            {
                return action == otherAction;
            }
        }
    }
}