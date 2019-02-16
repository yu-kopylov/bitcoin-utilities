using BitcoinUtilities.GUI.ViewModels;
using BitcoinUtilities.GUI.Views.Components;
using Eto.Forms;

namespace BitcoinUtilities.GUI.Views
{
    public class BitcoinNodeView : Panel
    {
        public BitcoinNodeView()
        {
            var propertiesTable = new PropertiesTable();
            propertiesTable.AddLabel("State:", nameof(ViewModel.State));
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

            Content = mainTable;
        }

        private BitcoinNodeViewModel ViewModel => DataContext as BitcoinNodeViewModel;

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
        }
    }
}