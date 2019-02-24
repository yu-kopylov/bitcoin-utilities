using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.GUI.ViewModels;
using BitcoinUtilities.GUI.Views.Components;
using Eto.Drawing;
using Eto.Forms;

namespace BitcoinUtilities.GUI.Views
{
    public class UtxoLookupView : TableLayout
    {
        public UtxoLookupView()
        {
            // todo: use spacing from some constant
            Spacing = new Size(5, 5);

            PropertiesTable criteriaTable = new PropertiesTable();
            criteriaTable.AddTextBox("Transaction ID", nameof(ViewModel.TransactionId));

            var buttonsPanel = new ButtonsPanel();
            buttonsPanel.AddButton("Search", () => ViewModel?.Search());

            // todo: use spacing from some constant
            TableLayout searchPanel = new TableLayout {Spacing = new Size(5, 5)};
            searchPanel.Rows.Add(new TableRow());
            searchPanel.Rows[0].Cells.Add(new TableCell(criteriaTable) {ScaleWidth = true});
            searchPanel.Rows[0].Cells.Add(buttonsPanel);

            GridView resultsGrid = new GridView();
            // todo: enable copy from grid
            resultsGrid.BindDataContext(
                new PropertyBinding<IEnumerable<object>>(nameof(resultsGrid.DataStore), false),
                new PropertyBinding<IEnumerable<object>>(nameof(ViewModel.UnspentOutputs), false)
            );

            resultsGrid.Columns.Add(new GridColumn {HeaderText = "Address", DataCell = new TextBoxCell(nameof(UtxoOutputViewModel.Address))});
            resultsGrid.Columns.Add(new GridColumn {HeaderText = "Value", DataCell = new TextBoxCell(nameof(UtxoOutputViewModel.FormattedValue))});
            resultsGrid.ContextMenu = new ContextMenu(new MenuItem[] {new ButtonMenuItem((_, __) => ViewModel?.AddToInputs(resultsGrid.SelectedItems.Cast<UtxoOutputViewModel>())) {Text = "Add to Inputs"}});

            Rows.Add(new TableRow(searchPanel));
            Rows.Add(new TableRow(resultsGrid) {ScaleHeight = true});
        }

        public UtxoLookupViewModel ViewModel
        {
            get { return DataContext as UtxoLookupViewModel; }
            set { DataContext = value; }
        }
    }
}