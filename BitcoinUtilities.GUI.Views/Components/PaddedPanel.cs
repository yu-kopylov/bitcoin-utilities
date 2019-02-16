using Eto.Forms;

namespace BitcoinUtilities.GUI.Views.Components
{
    public class PaddedPanel : Panel
    {
        public PaddedPanel()
        {
            Padding = 5;
        }

        public PaddedPanel(Control content) : this()
        {
            Content = content;
        }
    }
}