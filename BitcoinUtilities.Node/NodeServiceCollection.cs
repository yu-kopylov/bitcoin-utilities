using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using BitcoinUtilities.P2P;
using NLog;

namespace BitcoinUtilities.Node
{
    internal class NodeServiceCollection
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly List<NodeServiceInfo> services = new List<NodeServiceInfo>();

        public void AddFactory(INodeServiceFactory serviceFactory)
        {
            NodeServiceInfo serviceInfo = new NodeServiceInfo();
            serviceInfo.ServiceFactory = serviceFactory;

            services.Add(serviceInfo);
        }

        public void CreateServices(BitcoinNode bitcoinNode, CancellationToken cancellationToken)
        {
            foreach (NodeServiceInfo serviceInfo in services)
            {
                serviceInfo.Service = serviceInfo.ServiceFactory.Create(bitcoinNode, cancellationToken);
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

        public void DisposeServices()
        {
            foreach (NodeServiceInfo serviceInfo in services)
            {
                IDisposable disposableService = serviceInfo.Service as IDisposable;
                if (disposableService != null)
                {
                    DisposeService(disposableService);
                }
                //todo: remove reference to service?
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

        public void OnNodeConnected(BitcoinEndpoint endpoint)
        {
            //todo: add test and maybe catch exceptions

            //todo: make sure this method is called before any messages are received by service

            foreach (NodeServiceInfo serviceInfo in services)
            {
                serviceInfo.Service.OnNodeConnected(endpoint);
            }
        }

        public void OnNodeDisconnected(BitcoinEndpoint endpoint)
        {
            //todo: add test and maybe catch exceptions

            //todo: make sure this method is called only after OnNodeConnected

            foreach (NodeServiceInfo serviceInfo in services)
            {
                serviceInfo.Service.OnNodeDisconnected(endpoint);
            }
        }

        public void ProcessMessage(BitcoinEndpoint endpoint, IBitcoinMessage message)
        {
            //todo: add test and catch maybe catch other exceptions
            try
            {
                foreach (NodeServiceInfo serviceInfo in services)
                {
                    serviceInfo.Service.ProcessMessage(endpoint, message);
                }
            }
            catch (BitcoinProtocolViolationException e)
            {
                logger.Error(e, "Remote node violated protecol rules ({0}).", endpoint.PeerInfo.IpEndpoint);
                //todo: is this a correct way to disconnect node?
                //todo: ban node ?
                endpoint.Dispose();
            }
        }

        private class NodeServiceInfo
        {
            public INodeServiceFactory ServiceFactory { get; set; }
            public INodeService Service { get; set; }
            public Thread Thread { get; set; }
        }
    }
}