using System;
using BitcoinUtilities.GUI.ViewModels;
using BitcoinUtilities.GUI.Views.Components;
using BitcoinUtilities.GUI.Views.Wallet;
using Eto.Forms;

namespace BitcoinUtilities.GUI.Views
{
    public class BitcoinNodeView : Panel
    {
        private readonly UtxoLookupView utxoLookupView = new UtxoLookupView();
        private readonly TransactionBuilderView transactionBuilderView = new TransactionBuilderView();
        private readonly WalletView walletView = new WalletView();

        public BitcoinNodeView()
        {
            var propertiesTable = new PropertiesTable();
            propertiesTable.AddLabel("State:", nameof(ViewModel.State));
            propertiesTable.AddLabel("Network:", nameof(ViewModel.Network));
            propertiesTable.AddLabel("Blockchain Height:", nameof(ViewModel.BlockchainHeight));
            propertiesTable.AddLabel("UTXO Height:", nameof(ViewModel.UtxoHeight));
            propertiesTable.AddLabel("Incoming Connections:", nameof(ViewModel.IncomingConnectionsCount));
            propertiesTable.AddLabel("Outgoing Connections:", nameof(ViewModel.OutgoingConnectionsCount));

            var buttonsPanel = new ButtonsPanel();
            buttonsPanel.AddButton("Start Node", StartNode, enabled: nameof(ViewModel.CanStartNode));
            buttonsPanel.AddButton("Stop Node", StopNode, enabled: nameof(ViewModel.CanStopNode));
            buttonsPanel.AddButton("Settings", ShowSettingsDialog);

            var mainTable = new TableLayout();

            mainTable.Rows.Add(new PaddedPanel(propertiesTable));
            mainTable.Rows.Add(new HorizontalDivider());
            mainTable.Rows.Add(new PaddedPanel(buttonsPanel));
            mainTable.Rows.Add(new Panel());

            var tabControl = new TabControl();

            tabControl.Pages.Add(new TabPage {Text = "Overview", Content = mainTable});
            tabControl.Pages.Add(new TabPage {Text = "Wallet", Content = walletView});
            tabControl.Pages.Add(new TabPage {Text = "Transaction Builder", Content = transactionBuilderView});
            tabControl.Pages.Add(new TabPage {Text = "UTXO Lookup", Content = utxoLookupView});

            Content = tabControl;
        }

        private BitcoinNodeViewModel ViewModel => DataContext as BitcoinNodeViewModel;

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            utxoLookupView.ViewModel = ViewModel?.UtxoLookup;
            transactionBuilderView.ViewModel = ViewModel?.TransactionBuilder;
            walletView.ViewModel = ViewModel?.Wallet;
        }

        private void StartNode()
        {
            ViewModel.StartNode();
        }

        private void StopNode()
        {
            ViewModel.StopNode();
        }

        private void ShowSettingsDialog()
        {
            SettingsDialog dialog = new SettingsDialog();
            dialog.DataContext = ViewModel.CreateSettingsViewModel();
            dialog.ShowModal(this);
            // todo: settings dialog creation and call to UpdateValues should be in the view-model
            ViewModel.UpdateValues();
        }
    }
}