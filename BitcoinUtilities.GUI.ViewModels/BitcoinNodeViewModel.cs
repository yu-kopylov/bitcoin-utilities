﻿using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using BitcoinUtilities.GUI.Models;
using BitcoinUtilities.Node;
using BitcoinUtilities.Storage.SQLite;

namespace BitcoinUtilities.GUI.ViewModels
{
    public class BitcoinNodeViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationContext applicationContext;
        private readonly IViewContext viewContext;

        private string state;

        private int bestHeaderHeight;
        private int bestChainHeight;

        private int incomingConnectionsCount;
        private int outgoingConnectionsCount;

        private bool canStartNode;
        private bool canStopNode;

        public BitcoinNodeViewModel(ApplicationContext applicationContext, IViewContext viewContext)
        {
            this.applicationContext = applicationContext;
            this.viewContext = viewContext;
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

        public int BestHeaderHeight
        {
            get { return bestHeaderHeight; }
            private set
            {
                bestHeaderHeight = value;
                OnPropertyChanged();
            }
        }

        public int BestChainHeight
        {
            get { return bestChainHeight; }
            set
            {
                bestChainHeight = value;
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void StartNode()
        {
            NetworkParameters networkParameters = NetworkParameters.BitcoinCoreMain;

            string dataFolder = Path.Combine(applicationContext.Settings.BlockchainFolder, networkParameters.Name);
            SQLiteBlockchainStorage storage = SQLiteBlockchainStorage.Open(dataFolder);

            BitcoinNode node = new BitcoinNode(networkParameters, dataFolder, storage);
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
            const string nodeStateChangedEventType = "NodeStateChanged";
            node.PropertyChanged += (sender, args) => applicationContext.EventManager.Notify(nodeStateChangedEventType);
            node.ConnectionCollection.Changed += () => applicationContext.EventManager.Notify(nodeStateChangedEventType);
            node.Blockchain.StateChanged += () => applicationContext.EventManager.Notify(nodeStateChangedEventType);

            //todo: updates are too frequent, consider adding a delay to EventManager
            applicationContext.EventManager.Watch(nodeStateChangedEventType, OnNodePropertyChanged);

            UpdateValues();
        }

        public void StopNode()
        {
            applicationContext.BitcoinNode.Stop();
            //todo: who should close storage ?
            IDisposable storage = applicationContext.BitcoinNode.Storage as IDisposable;
            storage?.Dispose();
        }

        private void OnNodePropertyChanged()
        {
            viewContext.Invoke(UpdateValues);
        }

        private void UpdateValues()
        {
            BitcoinNode node = applicationContext.BitcoinNode;
            if (node == null)
            {
                State = "No Node";
                BestHeaderHeight = 0;
                BestChainHeight = 0;
                IncomingConnectionsCount = 0;
                OutgoingConnectionsCount = 0;
                CanStartNode = true;
                CanStopNode = false;
                return;
            }

            State = node.Started ? "Started" : "Stopped";
            BestHeaderHeight = node.Blockchain?.State?.BestHeader?.Height ?? 0;
            BestChainHeight = node.Blockchain?.State?.BestChain?.Height ?? 0;
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