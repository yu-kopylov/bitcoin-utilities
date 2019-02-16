using BitcoinUtilities.GUI.ViewModels;
using BitcoinUtilities.GUI.Views.Components;

namespace BitcoinUtilities.GUI.Views
{
    public class SettingsDialog : EmptyDialog
    {
        public SettingsDialog()
        {
            Title = "Settings";

            var propertiesTable = new PropertiesTable();
            propertiesTable.AddFolder("Blockchain Folder", nameof(Settings.BlockchainFolder));
            propertiesTable.AddFolder("Wallet Folder", nameof(Settings.WalletFolder));
            ContentPanel.Content = propertiesTable;

            DefaultButton = ButtonsPanel.AddButton("OK", ApplyAndClose);
            AbortButton = ButtonsPanel.AddButton("Cancel", Close);
            ButtonsPanel.AddButton("Set Defaults", SetDefaults);
        }

        private SettingsViewModel Settings => DataContext as SettingsViewModel;

        private void ApplyAndClose()
        {
            //todo: warn if restart is required
            Settings.Apply();
            Close();
        }

        private void SetDefaults()
        {
            Settings.SetDefaults();
        }
    }
}