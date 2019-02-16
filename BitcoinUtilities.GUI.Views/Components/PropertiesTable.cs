using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;

namespace BitcoinUtilities.GUI.Views.Components
{
    public class PropertiesTable : TableLayout
    {
        public PropertiesTable()
        {
            Spacing = new Size(5, 5);
        }

        public void AddLabel(string caption, string propertyName)
        {
            var binding = new PropertyBinding<string>(propertyName, ignoreCase: false);
            var label = new Label();
            label.TextBinding.BindDataContext(binding);
            AddRow(caption, label);
        }

        public void AddDropDown(string caption, string propertyName, string listPropertyName)
        {
            var valueBinding = new PropertyBinding<object>(propertyName, ignoreCase: false);
            var listBinding = new PropertyBinding<IEnumerable<object>>(listPropertyName, ignoreCase: false);
            var dataStoreBinding = new PropertyBinding<IEnumerable<object>>(nameof(DropDown.DataStore));

            var dropDown = new DropDown();
            dropDown.SelectedValueBinding.BindDataContext(valueBinding);
            dropDown.BindDataContext(dataStoreBinding, listBinding);

            AddRow(caption, dropDown);
        }

        public void AddFolder(string caption, string propertyName)
        {
            var binding = new PropertyBinding<string>(propertyName, ignoreCase: false);
            var textBox = new TextBox();
            var browseButton = new Button {Text = "Browse..."};

            textBox.TextBinding.BindDataContext(binding);
            browseButton.Click += (_, __) =>
            {
                var folderDialog = new SelectFolderDialog();
                folderDialog.Directory = textBox.TextBinding.DataValue;
                if (folderDialog.ShowDialog(this) == DialogResult.Ok)
                {
                    textBox.TextBinding.DataValue = folderDialog.Directory;
                }
            };

            var inputAndButtonTable = new TableLayout();
            inputAndButtonTable.Spacing = new Size(5, 5);
            inputAndButtonTable.Rows.Add(new TableRow(new TableCell(textBox) {ScaleWidth = true}, new TableCell(browseButton)));

            AddRow(caption, inputAndButtonTable);
        }

        private void AddRow(string labelText, Control control)
        {
            var label = new Label {Text = labelText, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center};
            var row = new TableRow();
            row.Cells.Add(new TableCell(label));
            row.Cells.Add(new TableCell(control) {ScaleWidth = true});
            Rows.Add(row);
        }
    }
}