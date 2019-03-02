using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.GUI.ViewModels.Wallet;
using BitcoinUtilities.GUI.Views.Components;
using Eto.Drawing;
using Eto.Forms;

namespace BitcoinUtilities.GUI.Views.Wallet
{
    public class WalletView : Panel
    {
        public WalletView()
        {
            TableLayout mainTable = new TableLayout {Spacing = new Size(5, 5)};

            var addressesGrid = new GridView();
            // todo: enable copy from grid
            addressesGrid.BindDataContext(
                new PropertyBinding<IEnumerable<object>>(nameof(addressesGrid.DataStore), false),
                new PropertyBinding<IEnumerable<object>>(nameof(ViewModel.Addresses), false)
            );

            addressesGrid.Columns.Add(new GridColumn {HeaderText = "Address", DataCell = new TextBoxCell(nameof(WalletAddressViewModel.Address))});
            addressesGrid.Columns.Add(new GridColumn {HeaderText = "Balance", DataCell = new TextBoxCell(nameof(WalletAddressViewModel.Balance))});
            addressesGrid.ContextMenu = new ContextMenu(new MenuItem[]
            {
                new ButtonMenuItem((_, __) => ViewModel?.AddToOutputs(addressesGrid.SelectedItems.Cast<WalletAddressViewModel>())) {Text = "Add to Outputs"}
            });

            var outputsGrid = new GridView();
            // todo: enable copy from grid
            outputsGrid.BindDataContext(
                new PropertyBinding<IEnumerable<object>>(nameof(outputsGrid.DataStore), false),
                new PropertyBinding<IEnumerable<object>>(nameof(ViewModel.Outputs), false)
            );

            outputsGrid.Columns.Add(new GridColumn {HeaderText = "Address", DataCell = new TextBoxCell(nameof(WalletOutputViewModel.Address))});
            outputsGrid.Columns.Add(new GridColumn {HeaderText = "Value", DataCell = new TextBoxCell(nameof(WalletOutputViewModel.FormattedValue))});
            outputsGrid.ContextMenu = new ContextMenu(new MenuItem[]
            {
                new ButtonMenuItem((_, __) => ViewModel?.AddToInputs(outputsGrid.SelectedItems.Cast<WalletOutputViewModel>())) {Text = "Add to Inputs"}
            });

            var buttonsPanel = new ButtonsPanel();
            buttonsPanel.AddButton("Add Address", ShowAddAddressDialog);

            mainTable.Rows.Add(buttonsPanel);
            mainTable.Rows.Add(new HorizontalDivider());
            mainTable.Rows.Add(new Label {Text = "Addresses"});
            mainTable.Rows.Add(new TableRow(addressesGrid) {ScaleHeight = true});
            mainTable.Rows.Add(new HorizontalDivider());
            mainTable.Rows.Add(new Label {Text = "Unspent Outputs"});
            mainTable.Rows.Add(new TableRow(outputsGrid) {ScaleHeight = true});

            this.Content = mainTable;
        }

        public WalletViewModel ViewModel
        {
            get { return DataContext as WalletViewModel; }
            set { DataContext = value; }
        }

        private void ShowAddAddressDialog()
        {
            var viewModel = ViewModel.CreateAddAddressViewModel();
            if (viewModel == null)
            {
                return;
            }

            var dialog = new AddWalletAddressDialog();
            dialog.ViewModel = viewModel;
            dialog.ShowModal(this);
        }
    }
}