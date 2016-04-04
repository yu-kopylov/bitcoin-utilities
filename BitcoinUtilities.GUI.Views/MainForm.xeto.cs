using System;
using System.Windows.Input;
using BitcoinUtilities.GUI.ViewModels;
using Eto.Forms;
using Eto.Serialization.Xaml;

namespace BitcoinUtilities.GUI.Views
{
    public class MainForm : Form
    {
        public MainForm(ApplicationContext applicationContext)
        {
            XamlReader.Load(this);

            DataContext = this;

            IViewContext viewContext = new ViewContext(this);

            BitcoinNode = new BitcoinNodeViewModel(applicationContext, viewContext);
            PaperWallet = new PaperWalletViewModel(viewContext);
        }

        public BitcoinNodeViewModel BitcoinNode { get; set; }
        public PaperWalletViewModel PaperWallet { get; set; }

        public ICommand ShowBioRandomDialog
        {
            get
            {
                return new Command((sender, e) =>
                {
                    byte[] randomValue;
                    using (var dialog = new BioRandomDialog())
                    {
                        dialog.ShowModal();
                        randomValue = dialog.SeedMeterial;
                    }

                    if (randomValue != null)
                    {
                        MessageBox.Show(
                            this,
                            string.Format("Generated Value:\n{0}", BitConverter.ToString(randomValue)),
                            "Random",
                            MessageBoxType.Information);
                    }
                });
            }
        }
    }
}