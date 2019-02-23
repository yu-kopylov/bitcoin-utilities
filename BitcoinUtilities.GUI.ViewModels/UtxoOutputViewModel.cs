using System.Globalization;
using BitcoinUtilities.Node.Modules.Outputs;
using BitcoinUtilities.Scripts;

namespace BitcoinUtilities.GUI.ViewModels
{
    public class UtxoOutputViewModel
    {
        public UtxoOutputViewModel(UtxoOutput output)
        {
            Output = output;
            Address = BitcoinScript.GetAddressFromPubkeyScript(output.PubkeyScript);
            FormattedValue = output.Value.ToString("0,000", CultureInfo.InvariantCulture);
        }

        public UtxoOutput Output { get; }
        public string Address { get; }
        public string FormattedValue { get; }
    }
}