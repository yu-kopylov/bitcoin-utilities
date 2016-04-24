using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NLog;

namespace BitcoinUtilities.Node
{
    internal class NodeServiceCollection : IDisposable
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly List<NodeServiceInfo> services = new List<NodeServiceInfo>();

        public void Dispose()
        {
            foreach (NodeServiceInfo serviceInfo in services)
            {
                IDisposable disposableService = serviceInfo.Service as IDisposable;
                if (disposableService != null)
                {
                    DisposeService(disposableService);
                }
            }
        }

        private void DisposeService(IDisposable service)
        {
            try
            {
                service.Dispose();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error diring disposal of a NodeService.");
            }
        }

        public void Add(INodeService service)
        {
            NodeServiceInfo serviceInfo = new NodeServiceInfo();
            serviceInfo.Service = service;

            services.Add(serviceInfo);
        }

        public void Init(BitcoinNode node, CancellationToken cancellationToken)
        {
            foreach (NodeServiceInfo serviceInfo in services)
            {
                serviceInfo.Service.Init(node, cancellationToken);
            }
        }

        public void Start()
        {
            foreach (NodeServiceInfo serviceInfo in services)
            {
                Thread thread = new Thread(serviceInfo.Service.Run);
                thread.IsBackground = true;

                serviceInfo.Thread = thread;

                thread.Start();
            }
        }

        public bool Join(TimeSpan timeout)
        {
            bool terminated = true;

            Stopwatch elapsed = Stopwatch.StartNew();

            foreach (NodeServiceInfo serviceInfo in services)
            {
                TimeSpan remainingTimeout = timeout - elapsed.Elapsed;
                if (remainingTimeout < TimeSpan.Zero)
                {
                    remainingTimeout = TimeSpan.Zero;
                }
                terminated &= serviceInfo.Thread.Join(remainingTimeout);
            }

            return terminated;
        }

        private class NodeServiceInfo
        {
            public INodeService Service { get; set; }
            public Thread Thread { get; set; }
        }
    }
}