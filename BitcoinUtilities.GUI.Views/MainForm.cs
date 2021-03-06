﻿using System;
using BitcoinUtilities.GUI.ViewModels;
using BitcoinUtilities.GUI.Views.Components;
using Eto.Drawing;
using Eto.Forms;

namespace BitcoinUtilities.GUI.Views
{
    public class MainForm : Form
    {
        private readonly PaperWalletView paperWalletView = new PaperWalletView();
        private readonly BitcoinNodeView bitcoinNodeView = new BitcoinNodeView();

        public MainForm()
        {
            Title = "Bitcoin Utilities";
            Size = new Size(800, 600);
            MinimumSize = new Size(600, 450);

            var tabControl = new TabControl {TabPosition = DockPosition.Left};
            tabControl.Pages.Add(new TabPage {Text = "Paper Wallet", Content = paperWalletView});

            var bioRandomDialogButton = new Button {Text = "Generate Random"};
            bioRandomDialogButton.Click += ShowBioRandomDialog;
            tabControl.Pages.Add(new TabPage {Text = "Random Number Generator", Content = new StackLayout {Items = {new PaddedPanel(bioRandomDialogButton)}}});

            tabControl.Pages.Add(new TabPage {Text = "Bitcoin Node", Content = bitcoinNodeView});

            Content = tabControl;
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            MainFormViewModel viewModel = DataContext as MainFormViewModel;
            paperWalletView.DataContext = viewModel?.PaperWallet;
            bitcoinNodeView.DataContext = viewModel?.BitcoinNode;
        }

        protected void ShowBioRandomDialog(object sender, EventArgs e)
        {
            byte[] randomValue;
            using (var dialog = new BioRandomDialog())
            {
                dialog.ShowModal();
                randomValue = dialog.SeedMaterial;
            }

            if (randomValue != null)
            {
                MessageBox.Show
                (
                    this,
                    $"Generated Value:\n{HexUtils.GetString(randomValue)}",
                    "Random",
                    MessageBoxType.Information
                );
            }
        }
    }
}