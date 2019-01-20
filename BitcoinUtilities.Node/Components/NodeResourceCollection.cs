using System;
using System.Collections.Generic;

namespace BitcoinUtilities.Node.Components
{
    /// <summary>
    /// A thread-safe collection of resources.
    /// </summary>
    public class NodeResourceCollection : IDisposable
    {
        private readonly object monitor = new object();

        private readonly Dictionary<Type, object> resources = new Dictionary<Type, object>();

        private volatile bool disposed;

        /// <summary>
        /// Disposes all associated resources.
        /// </summary>
        public void Dispose()
        {
            lock (monitor)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;

                foreach (var resource in resources.Values)
                {
                    if (resource is IDisposable disposableResource)
                    {
                        disposableResource.Dispose();
                    }
                }
            }
        }

        public void Add<T>(T resource)
        {
            lock (monitor)
            {
                if (disposed)
                {
                    throw new InvalidOperationException($"Cannot add resource to a disposed {nameof(NodeResourceCollection)}.");
                }

                resources.Add(typeof(T), resource);
            }
        }

        public T Get<T>()
        {
            lock (monitor)
            {
                Type resourceType = typeof(T);
                if (!resources.TryGetValue(resourceType, out var resource))
                {
                    throw new InvalidOperationException($"The {nameof(NodeResourceCollection)} does not have a resource of type {resourceType.Name}.");
                }

                return (T) resource;
            }
        }
    }
}