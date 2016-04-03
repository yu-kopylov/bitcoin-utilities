using Eto.Forms;
using Eto.Serialization.Xaml;

namespace BitcoinUtilities.GUI.Views
{
    public class BitcoinNodeView : Panel
    {
        public BitcoinNodeView()
        {
            XamlReader.Load(this);
        }
    }
}
