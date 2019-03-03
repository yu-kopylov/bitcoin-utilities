using System.Globalization;
using BitcoinUtilities.Node.Modules.Outputs;
using BitcoinUtilities.Scripts;

namespace BitcoinUtilities.GUI.ViewModels
{
    public class UtxoOutputViewModel
    {
        public UtxoOutputViewModel(NetworkParameters networkParameters, UtxoOutput output)
        {
            Output = output;
            byte[] publicKeyHash = BitcoinScript.GetPublicKeyHashFromPubkeyScript(output.PubkeyScript);
            Address = publicKeyHash == null ? null : networkParameters.AddressConverter.ToDefaultAddress(publicKeyHash);
            FormattedValue = output.Value.ToString("0,000", CultureInfo.InvariantCulture);
        }

        public UtxoOutput Output { get; }
        public string Address { get; }
        public string FormattedValue { get; }
    }
}