﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BitcoinUtilities.Node.Modules.Outputs;

namespace BitcoinUtilities.GUI.ViewModels
{
    public class UtxoLookupViewModel : INotifyPropertyChanged
    {
        private readonly IViewContext viewContext;

        private string transactionId;

        public UtxoLookupViewModel(IViewContext viewContext, BitcoinNodeViewModel node)
        {
            this.viewContext = viewContext;
            Node = node;
        }

        public string TransactionId
        {
            get { return transactionId; }
            set
            {
                transactionId = value;
                OnPropertyChanged();
            }
        }

        private BitcoinNodeViewModel Node { get; }

        public ObservableCollection<UtxoOutputViewModel> UnspentOutputs { get; } = new ObservableCollection<UtxoOutputViewModel>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Search()
        {
            if (!HexUtils.TryGetReversedBytes(transactionId, out var txHash))
            {
                viewContext.ShowError("Invalid transaction ID.");
                return;
            }

            var bitcoinNode = Node.BitcoinNode;
            if (bitcoinNode == null || !bitcoinNode.Started)
            {
                // todo: also detect when node is stopping
                viewContext.ShowError("Bitcoin node is not started.");
                return;
            }

            UnspentOutputs.Clear();
            var outputs = bitcoinNode.Resources.Get<UtxoRepository>().GetUnspentOutputs(new HashSet<byte[]>(ByteArrayComparer.Instance) {txHash});
            foreach (var output in outputs)
            {
                UnspentOutputs.Add(new UtxoOutputViewModel(output));
            }
        }
    }
}