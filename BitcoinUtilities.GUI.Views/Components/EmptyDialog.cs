using Eto.Forms;

namespace BitcoinUtilities.GUI.Views.Components
{
    public class EmptyDialog : Dialog
    {
        public EmptyDialog()
        {
            Resizable = true;

            var mainTable = new TableLayout {Width = 600};
            mainTable.Rows.Add(new TableRow(ContentPanel));
            mainTable.Rows.Add(new TableRow {ScaleHeight = true});
            mainTable.Rows.Add(new TableRow(new HorizontalDivider()));
            mainTable.Rows.Add(new TableRow(new PaddedPanel(ButtonsPanel)));
            Content = mainTable;
        }

        public Panel ContentPanel { get; } = new PaddedPanel();
        public ButtonsPanel ButtonsPanel { get; } = new ButtonsPanel();
    }
}