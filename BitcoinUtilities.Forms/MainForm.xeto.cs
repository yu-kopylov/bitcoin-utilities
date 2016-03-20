using System;
using System.Windows.Input;
using Eto.Forms;
using Eto.Serialization.Xaml;

namespace BitcoinUtilities.Forms
{
    public class MainForm : Form
    {
        public MainForm()
        {
            XamlReader.Load(this);

            DataContext = this;
        }

        public ICommand ShowBip38EncoderDialog
        {
            get
            {
                return new Command((sender, e) =>
                {
                    using (var dialog = new Bip38EncoderDialog())
                    {
                        dialog.ShowModal();
                    }
                });
            }
        }

        public ICommand ShowBioRandomDialog
        {
            get
            {
                return new Command((sender, e) =>
                {
                    byte[] randomValue;
                    using (var dialog = new BioRandomDialog(32))
                    {
                        dialog.ShowModal();
                        randomValue = dialog.RandomValue;
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