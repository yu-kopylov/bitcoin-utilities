using System;
using Eto.Drawing;
using Eto.Forms;

namespace BitcoinUtilities.GUI.Views.Components
{
    public class ButtonsPanel : TableLayout
    {
        private readonly TableRow buttonsRow;

        public ButtonsPanel()
        {
            Spacing = new Size(5, 5);
            buttonsRow = new TableRow();
            Rows.Add(buttonsRow);
            buttonsRow.Cells.Add(new TableCell {ScaleWidth = true});
        }

        public Button AddButton(string text, Action action)
        {
            var button = new Button {Text = text, Width = 90};
            button.Click += (sender, args) => action();
            buttonsRow.Cells.Add(button);
            return button;
        }
    }
}