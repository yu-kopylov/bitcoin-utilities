using Eto.Drawing;
using Eto.Forms;

namespace BitcoinUtilities.GUI.Views.Components
{
    // todo: remove?
    public class HeaderPanel : Panel
    {
        public HeaderPanel(string text)
        {
            Padding = 2;
            BackgroundColor = SystemColors.Highlight;
            Content = new Label {Text = text, TextColor = SystemColors.HighlightText};
        }
    }
}