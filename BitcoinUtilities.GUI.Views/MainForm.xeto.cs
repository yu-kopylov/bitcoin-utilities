using System;
using Eto.Forms;
using Eto.Serialization.Xaml;

namespace BitcoinUtilities.GUI.Views
{
    public class MainForm : Form
    {
        public MainForm()
        {
            XamlReader.Load(this);
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
                MessageBox.Show(
                    this,
                    $"Generated Value:\n{BitConverter.ToString(randomValue)}",
                    "Random",
                    MessageBoxType.Information);
            }
        }
    }
}