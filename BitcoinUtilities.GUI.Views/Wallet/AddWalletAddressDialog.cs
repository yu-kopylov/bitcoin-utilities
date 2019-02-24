using BitcoinUtilities.GUI.ViewModels.Wallet;
using BitcoinUtilities.GUI.Views.Components;

namespace BitcoinUtilities.GUI.Views.Wallet
{
    public class AddWalletAddressDialog : EmptyDialog
    {
        public AddWalletAddressDialog()
        {
            Title = "Add Address";
            ContentPanel.Width = 600;

            PropertiesTable addressTable = new PropertiesTable();
            addressTable.AddTextBox("WIF", nameof(ViewModel.Wif));

            DefaultButton = ButtonsPanel.AddButton("Add", AddAndClose);
            AbortButton = ButtonsPanel.AddButton("Cancel", Close);

            ContentPanel.Content = addressTable;
        }

        private void AddAndClose()
        {
            if (ViewModel.AddAddress())
            {
                Close();
            }
        }

        public AddWalletAddressViewModel ViewModel
        {
            get { return DataContext as AddWalletAddressViewModel; }
            set { DataContext = value; }
        }
    }
}