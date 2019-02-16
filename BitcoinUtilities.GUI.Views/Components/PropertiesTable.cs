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

        public void AddFolder(string labelText, string propertyName)
        {
            var binding = new PropertyBinding<string>(propertyName);
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

            AddRow(labelText, inputAndButtonTable);
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