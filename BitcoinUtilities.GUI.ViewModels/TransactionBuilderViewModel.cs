using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Scripts;

namespace BitcoinUtilities.GUI.ViewModels
{
    public class TransactionBuilderViewModel
    {
        public TransactionBuilderViewModel(IViewContext viewContext, BitcoinNodeViewModel node)
        {
            ViewContext = viewContext;
            Node = node;
        }

        private IViewContext ViewContext { get; }
        public BitcoinNodeViewModel Node { get; }

        public ObservableCollection<TransactionInputViewModel> Inputs { get; } = new ObservableCollection<TransactionInputViewModel>();
        public ObservableCollection<TransactionOutputViewModel> Outputs { get; } = new ObservableCollection<TransactionOutputViewModel>();

        public void AddInput(TxOutPoint outPoint, ulong value, byte[] pubkeyScript, byte[] privateKey = null)
        {
            // todo: use real implementation
            var networkKind = Node.BitcoinNode.NetworkParameters.Name.Contains("test") ? BitcoinNetworkKind.Test : BitcoinNetworkKind.Main;

            var input = new TransactionInputViewModel(outPoint, value, pubkeyScript);
            if (privateKey != null)
            {
                // todo: use network parameters
                input.Wif = Wif.Encode(networkKind, privateKey, true);
            }

            Inputs.Add(input);
        }

        public void AddOutput(string address, ulong value)
        {
            Outputs.Add(new TransactionOutputViewModel {Address = address, Value = value});
        }

        public void Create()
        {
            var bitcoinNode = Node.GetStartedNodeOrShowError();
            if (bitcoinNode == null)
            {
                return;
            }

            // todo: use real implementation
            var networkKind = bitcoinNode.NetworkParameters.Name.Contains("test") ? BitcoinNetworkKind.Test : BitcoinNetworkKind.Main;

            TransactionBuilder builder = new TransactionBuilder(bitcoinNode.NetworkParameters.Fork);
            foreach (var input in Inputs)
            {
                if (!Wif.TryDecode(networkKind, input.Wif, out var privateKey, out var compressed))
                {
                    ViewContext.ShowError("Invalid WIF encoding.");
                    return;
                }

                builder.AddInput(input.OutPoint.Hash, input.OutPoint.Index, input.PubkeyScript, input.Value, privateKey, compressed);
            }

            foreach (var output in Outputs)
            {
                builder.AddOutput(BitcoinScript.CreatePayToPubkeyHash(output.Address), output.Value);
            }


            Tx tx;
            try
            {
                tx = builder.Build();
            }
            catch (InvalidOperationException ex)
            {
                ViewContext.ShowError(ex);
                return;
            }

            // todo: broadcast / ShowInfo
            ViewContext.ShowError(HexUtils.GetString(BitcoinStreamWriter.GetBytes(tx.Write)));
        }
    }

    public class TransactionInputViewModel : INotifyPropertyChanged
    {
        private string wif;

        public TransactionInputViewModel(TxOutPoint outPoint, ulong value, byte[] pubkeyScript)
        {
            OutPoint = outPoint;
            TransactionId = HexUtils.GetReversedString(outPoint.Hash);
            OutputIndex = outPoint.Index;

            Value = value;
            FormattedValue = value.ToString("0,000", CultureInfo.InvariantCulture);

            PubkeyScript = pubkeyScript;
            // todo: use network parameters
            Address = BitcoinScript.GetAddressFromPubkeyScript(BitcoinNetworkKind.Main, pubkeyScript);
        }

        public TxOutPoint OutPoint { get; }
        public string TransactionId { get; }
        public int OutputIndex { get; }

        public byte[] PubkeyScript { get; }
        public string Address { get; }

        public ulong Value { get; }
        public string FormattedValue { get; }

        public string Wif
        {
            get { return wif; }
            set
            {
                this.wif = value;
                OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TransactionOutputViewModel : INotifyPropertyChanged
    {
        private string address;
        private ulong value;

        public string Address
        {
            get { return address; }
            set
            {
                address = value;
                OnPropertyChanged();
            }
        }

        public ulong Value
        {
            get { return value; }
            set
            {
                this.value = value;
                OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}