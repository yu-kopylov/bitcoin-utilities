using System;
using BitcoinUtilities.GUI.ViewModels;
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

        protected void StartNode(object sender, EventArgs e)
        {
            BitcoinNodeViewModel viewModel = (BitcoinNodeViewModel) DataContext;
            viewModel.StartNode();
        }

        protected void StopNode(object sender, EventArgs e)
        {
            BitcoinNodeViewModel viewModel = (BitcoinNodeViewModel) DataContext;
            viewModel.StopNode();
        }

        protected void ShowSettings(object sender, EventArgs e)
        {
            BitcoinNodeViewModel viewModel = (BitcoinNodeViewModel) DataContext;

            SettingsDialog dialog = new SettingsDialog();

            SettingsViewModel settingsViewModel = viewModel.CreateSettingsViewModel();
            dialog.DataContext = settingsViewModel;

            dialog.ShowModal(this);
        }
    }
}