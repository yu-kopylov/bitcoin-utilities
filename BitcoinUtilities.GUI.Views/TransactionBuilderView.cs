using System.Collections.Generic;
using BitcoinUtilities.GUI.ViewModels;
using BitcoinUtilities.GUI.Views.Components;
using Eto.Drawing;
using Eto.Forms;

namespace BitcoinUtilities.GUI.Views
{
    public class TransactionBuilderView : TableLayout
    {
        public TransactionBuilderView()
        {
            Spacing = new Size(5, 5);

            GridView inputsGrid = new GridView();
            // todo: enable copy from grid
            // todo: enable deletion
            inputsGrid.BindDataContext(
                new PropertyBinding<IEnumerable<object>>(nameof(inputsGrid.DataStore), false),
                new PropertyBinding<IEnumerable<object>>(nameof(ViewModel.Inputs), false)
            );

            inputsGrid.Columns.Add(new GridColumn {HeaderText = "Transaction ID", DataCell = new TextBoxCell(nameof(TransactionInputViewModel.TransactionId))});
            inputsGrid.Columns.Add(new GridColumn {HeaderText = "Output Index", DataCell = new TextBoxCell(nameof(TransactionInputViewModel.OutputIndex))});
            inputsGrid.Columns.Add(new GridColumn {HeaderText = "Address", DataCell = new TextBoxCell(nameof(TransactionInputViewModel.Address))});
            inputsGrid.Columns.Add(new GridColumn {HeaderText = "Value", DataCell = new TextBoxCell(nameof(TransactionInputViewModel.FormattedValue))});
            inputsGrid.Columns.Add(new GridColumn {HeaderText = "WIF", DataCell = new TextBoxCell(nameof(TransactionInputViewModel.Wif)), Editable = true});

            GridView outputsGrid = new GridView();
            // todo: enable copy from grid
            // todo: enable adding new rows
            // todo: enable deletion
            outputsGrid.BindDataContext(
                new PropertyBinding<IEnumerable<object>>(nameof(outputsGrid.DataStore), false),
                new PropertyBinding<IEnumerable<object>>(nameof(ViewModel.Outputs), false)
            );

            outputsGrid.Columns.Add(new GridColumn {HeaderText = "Address", DataCell = new TextBoxCell(nameof(TransactionOutputViewModel.Address)), Editable = true});
            outputsGrid.Columns.Add(new GridColumn {HeaderText = "Value", DataCell = new TextBoxCell(nameof(TransactionOutputViewModel.Value)), Editable = true});

            ButtonsPanel buttonsPanel = new ButtonsPanel();
            buttonsPanel.AddButton("Create", () => ViewModel?.Create());

            Rows.Add(new Label {Text = "Inputs:"});
            Rows.Add(new TableRow(inputsGrid) {ScaleHeight = true});
            Rows.Add(new Label {Text = "Outputs:"});
            Rows.Add(new TableRow(outputsGrid) {ScaleHeight = true});
            Rows.Add(buttonsPanel);
        }

        public TransactionBuilderViewModel ViewModel
        {
            get { return DataContext as TransactionBuilderViewModel; }
            set { DataContext = value; }
        }
    }
}