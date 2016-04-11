using BitcoinUtilities.Node;

namespace BitcoinUtilities.GUI.Models
{
    public class ApplicationContext
    {
        private BitcoinNode bitcoinNode;

        public BitcoinNode BitcoinNode
        {
            get { return bitcoinNode; }
            set { bitcoinNode = value; }
        }
    }
}