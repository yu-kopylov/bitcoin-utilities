using System;
using BitcoinUtilities.GUI.ViewModels;
using Eto.Forms;
using Eto.Serialization.Xaml;

namespace BitcoinUtilities.GUI.Views
{
    public class SettingsDialog : Dialog
    {
        public SettingsDialog()
        {
            XamlReader.Load(this);
        }

        protected void DefaultButton_Click(object sender, EventArgs e)
        {
            SettingsViewModel viewModel = (SettingsViewModel) DataContext;
            viewModel.Apply();
            //todo: warn is restart is required
            Close();
        }

        protected void AbortButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        protected void SetDefaults(object sender, EventArgs e)
        {
            SettingsViewModel viewModel = (SettingsViewModel)DataContext;
            viewModel.SetDefaults();
        }

        protected void SelectBlockchainFolder(object sender, EventArgs e)
        {
            SelectFolderDialog folderDialog = new SelectFolderDialog();
            if (folderDialog.ShowDialog(this) == DialogResult.Ok)
            {
                SettingsViewModel viewModel = (SettingsViewModel)DataContext;
                viewModel.BlockchainFolder = folderDialog.Directory;
            }
        }

        protected void SelectWalletFolder(object sender, EventArgs e)
        {
            SelectFolderDialog folderDialog = new SelectFolderDialog();
            if (folderDialog.ShowDialog(this) == DialogResult.Ok)
            {
                SettingsViewModel viewModel = (SettingsViewModel)DataContext;
                viewModel.WalletFolder = folderDialog.Directory;
            }
        }
    }
}
