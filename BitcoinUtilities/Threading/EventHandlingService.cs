using System;
using System.Collections.Generic;

namespace BitcoinUtilities.Threading
{
    public abstract class EventHandlingService : IEventHandlingService
    {
        private readonly List<Tuple<Predicate<object>, Action<object>>> handlers = new List<Tuple<Predicate<object>, Action<object>>>();

        public virtual void OnStart()
        {
        }

        public virtual void OnTearDown()
        {
        }

        public Action<object> GetHandler(object evt)
        {
            foreach (var handler in handlers)
            {
                if (handler.Item1(evt))
                {
                    return handler.Item2;
                }
            }

            return null;
        }

        protected void On<T>(Action<T> handler)
        {
            handlers.Add(Tuple.Create<Predicate<object>, Action<object>>(evt => evt is T, evt => handler((T) evt)));
        }

        protected void On<T>(Predicate<T> condition, Action<T> handler)
        {
            handlers.Add(Tuple.Create<Predicate<object>, Action<object>>(evt => evt is T typedEvent && condition(typedEvent), evt => handler((T) evt)));
        }
    }
}