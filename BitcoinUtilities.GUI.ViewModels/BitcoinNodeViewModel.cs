using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using BitcoinUtilities.GUI.Models;
using BitcoinUtilities.Node;

namespace BitcoinUtilities.GUI.ViewModels
{
    public class BitcoinNodeViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationContext applicationContext;
        private readonly IViewContext viewContext;

        private string state;
        private string network;

        private int blockchainHeight;
        private int utxoHeight;

        private int incomingConnectionsCount;
        private int outgoingConnectionsCount;

        private bool canStartNode;
        private bool canStopNode;

        public BitcoinNodeViewModel(ApplicationContext applicationContext, IViewContext viewContext)
        {
            this.applicationContext = applicationContext;
            this.viewContext = viewContext;
            this.UtxoLookup = new UtxoLookupViewModel(viewContext, this);
            UpdateValues();
        }

        public string State
        {
            get { return state; }
            private set
            {
                state = value;
                OnPropertyChanged();
            }
        }

        public string Network
        {
            get { return network; }
            set
            {
                network = value;
                OnPropertyChanged();
            }
        }

        public int BlockchainHeight
        {
            get { return blockchainHeight; }
            private set
            {
                blockchainHeight = value;
                OnPropertyChanged();
            }
        }

        public int UtxoHeight
        {
            get { return utxoHeight; }
            set
            {
                utxoHeight = value;
                OnPropertyChanged();
            }
        }

        public int IncomingConnectionsCount
        {
            get { return incomingConnectionsCount; }
            set
            {
                incomingConnectionsCount = value;
                OnPropertyChanged();
            }
        }

        public int OutgoingConnectionsCount
        {
            get { return outgoingConnectionsCount; }
            set
            {
                outgoingConnectionsCount = value;
                OnPropertyChanged();
            }
        }

        public bool CanStartNode
        {
            get { return canStartNode; }
            private set
            {
                canStartNode = value;
                OnPropertyChanged();
            }
        }

        public bool CanStopNode
        {
            get { return canStopNode; }
            private set
            {
                canStopNode = value;
                OnPropertyChanged();
            }
        }

        // todo: this view should have its own node, to allow multiple nodes
        public BitcoinNode BitcoinNode => applicationContext.BitcoinNode;

        public UtxoLookupViewModel UtxoLookup { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void StartNode()
        {
            if (!NetworkParameters.TryGetByName(applicationContext.Settings.Network, out var networkParameters))
            {
                viewContext.ShowError($"Unknown network name: {applicationContext.Settings.Network ?? "null"}.");
                return;
            }

            string dataFolder = Path.Combine(applicationContext.Settings.BlockchainFolder, networkParameters.Name);
            BitcoinNode node = new BitcoinNode(networkParameters, dataFolder);

            const string nodeStateChangedEventType = "NodeStateChanged";
            // todo: rethink service -> UI notifications patterns
            node.AddModule(new UIModule(applicationContext, viewContext, this, nodeStateChangedEventType));

            try
            {
                node.Start();
            }
            catch (Exception ex)
            {
                viewContext.ShowError(ex);
                //todo: dispose storage?
                return;
            }

            BitcoinNode oldNode = applicationContext.BitcoinNode;
            applicationContext.BitcoinNode = node;
            if (oldNode != null)
            {
                //todo: unregister handlers?
            }

            //todo: unregister handlers?
            node.PropertyChanged += (sender, args) => applicationContext.EventManager.Notify(nodeStateChangedEventType);

            //todo: updates are too frequent, consider adding a delay to EventManager
            applicationContext.EventManager.Watch(nodeStateChangedEventType, OnNodePropertyChanged);

            UpdateValues();
        }

        public void StopNode()
        {
            applicationContext.BitcoinNode.Stop();
        }

        private void OnNodePropertyChanged()
        {
            viewContext.Invoke(UpdateValues);
        }

        public void UpdateValues()
        {
            BitcoinNode node = applicationContext.BitcoinNode;
            if (node == null)
            {
                State = "No Node";
                Network = applicationContext.Settings.Network;
                BlockchainHeight = 0;
                UtxoHeight = 0;
                IncomingConnectionsCount = 0;
                OutgoingConnectionsCount = 0;
                CanStartNode = true;
                CanStopNode = false;
                return;
            }

            State = node.Started ? "Started" : "Stopped";
            Network = node.NetworkParameters.Name;
            BlockchainHeight = node.Blockchain?.GetBestHead()?.Height ?? 0;
            // todo: BestChainHeight = node.UtxoStorage?.GetLastHeader()?.Height ?? 0; (thread-safe?)
            IncomingConnectionsCount = node.ConnectionCollection.IncomingConnectionsCount;
            OutgoingConnectionsCount = node.ConnectionCollection.OutgoingConnectionsCount;
            CanStartNode = !node.Started;
            CanStopNode = node.Started;
        }

        public SettingsViewModel CreateSettingsViewModel()
        {
            return new SettingsViewModel(applicationContext);
        }
    }
}