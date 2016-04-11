﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BitcoinUtilities.GUI.Models;
using BitcoinUtilities.Node;
using BitcoinUtilities.Storage;

namespace BitcoinUtilities.GUI.ViewModels
{
    public class BitcoinNodeViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationContext applicationContext;
        private readonly IViewContext viewContext;

        private string state;

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
            //todo: use settings for storage location
            BlockChainStorage storage = BlockChainStorage.Open(@"D:\Temp\Blockchain");
            BitcoinNode node = new BitcoinNode(storage);
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
                oldNode.PropertyChanged -= OnNodePropertyChanged;
            }
            node.PropertyChanged += OnNodePropertyChanged;
            node.ConnectionCollection.Changed += () => OnNodePropertyChanged(null, null); //todo: unregister?
            UpdateValues();
        }

        public void StopNode()
        {
            applicationContext.BitcoinNode.Stop();
            //todo: who should close storage ?
            IDisposable storage = applicationContext.BitcoinNode.Storage as IDisposable;
            storage?.Dispose();
        }

        private void OnNodePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            viewContext.Invoke(UpdateValues);
        }

        private void UpdateValues()
        {
            BitcoinNode node = applicationContext.BitcoinNode;
            if (node == null)
            {
                State = "No Node";
                IncomingConnectionsCount = 0;
                OutgoingConnectionsCount = 0;
                CanStartNode = true;
                CanStopNode = false;
                return;
            }
            State = node.Started ? "Started" : "Stopped";
            IncomingConnectionsCount = node.ConnectionCollection.IncomingConnectionsCount;
            OutgoingConnectionsCount = node.ConnectionCollection.OutgoingConnectionsCount;
            CanStartNode = !node.Started;
            CanStopNode = node.Started;
        }
    }
}