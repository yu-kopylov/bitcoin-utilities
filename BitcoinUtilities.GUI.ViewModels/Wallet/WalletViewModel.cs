using System.Collections.Generic;
using System.Collections.ObjectModel;
using BitcoinUtilities.Node;
using BitcoinUtilities.Node.Modules.Wallet;

namespace BitcoinUtilities.GUI.ViewModels.Wallet
{
    public class WalletViewModel
    {
        private readonly IViewContext viewContext;
        private readonly BitcoinNodeViewModel node;

        public WalletViewModel(IViewContext viewContext, BitcoinNodeViewModel node)
        {
            this.viewContext = viewContext;
            this.node = node;
        }

        public ObservableCollection<WalletAddressViewModel> Addresses { get; } = new ObservableCollection<WalletAddressViewModel>();
        public ObservableCollection<WalletOutputViewModel> Outputs { get; } = new ObservableCollection<WalletOutputViewModel>();

        public void AddToInputs(IEnumerable<WalletOutputViewModel> outputs)
        {
            BitcoinNode bitcoinNode = node.GetStartedNodeOrShowError();
            var wallet = bitcoinNode.Resources.Get<Node.Modules.Wallet.Wallet>();
            if (wallet == null)
            {
                return;
            }

            foreach (var output in outputs)
            {
                node.TransactionBuilder.AddInput(output.OutPoint, output.Value, output.PubkeyScript, wallet.GetPrivateKey(output.PublicKeyHash));
            }
        }

        public void AddToOutputs(IEnumerable<WalletAddressViewModel> addresses)
        {
            BitcoinNode bitcoinNode = node.GetStartedNodeOrShowError();
            if (bitcoinNode == null)
            {
                return;
            }

            foreach (var address in addresses)
            {
                node.TransactionBuilder.AddOutput(address.Address, 0);
            }
        }

        public void AddPrivateKey(byte[] privateKey)
        {
            BitcoinNode bitcoinNode = node.GetStartedNodeOrShowError();
            bitcoinNode.Resources.Get<Node.Modules.Wallet.Wallet>().AddPrivateKey(privateKey);
        }

        public AddWalletAddressViewModel CreateAddAddressViewModel()
        {
            BitcoinNode bitcoinNode = node.GetStartedNodeOrShowError();
            if (bitcoinNode == null)
            {
                return null;
            }

            return new AddWalletAddressViewModel(viewContext, bitcoinNode.NetworkParameters, this);
        }

        public void Update(NetworkParameters networkParameters, IReadOnlyList<WalletAddress> addresses, IReadOnlyList<WalletOutput> outputs)
        {
            Addresses.Clear();
            foreach (var address in addresses)
            {
                Addresses.Add(new WalletAddressViewModel(networkParameters, address));
            }

            Outputs.Clear();
            foreach (var output in outputs)
            {
                Outputs.Add(new WalletOutputViewModel(networkParameters, output.PublicKeyHash, output.OutPoint.Hash, output.OutPoint.Index, output.Value, output.PubkeyScript));
            }
        }
    }
}