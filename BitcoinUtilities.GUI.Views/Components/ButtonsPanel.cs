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

        public Button AddButton(string text, Action action, string enabled = null)
        {
            var button = new Button {Text = text, Width = 90};
            button.Click += (sender, args) => action();

            if (enabled != null)
            {
                var controlBinding = new PropertyBinding<bool>(nameof(Button.Enabled), ignoreCase: false);
                var valueBinding = new PropertyBinding<bool>(enabled, ignoreCase: false);
                button.BindDataContext(controlBinding, valueBinding);
            }

            buttonsRow.Cells.Add(button);
            return button;
        }
    }
}