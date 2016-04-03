using System;
using System.Windows.Input;
using BitcoinUtilities.GUI.ViewModels;
using BitcoinUtilities.Storage;
using Eto.Forms;
using Eto.Serialization.Xaml;

namespace BitcoinUtilities.GUI.Views
{
    public class MainForm : Form
    {
        public MainForm()
        {
            XamlReader.Load(this);

            DataContext = this;
        }

        public BitcoinNodeViewModel BitcoinNode { get; set; } = new BitcoinNodeViewModel();

        public ICommand StartNode
        {
            get
            {
                return new Command((sender, e) =>
                {
                    if (BitcoinNode.Node == null || !BitcoinNode.Node.Started)
                    {
                        try
                        {
                            //todo: use settings for storage location
                            BlockChainStorage storage = BlockChainStorage.Open(@"D:\Temp\Blockchain");
                            BitcoinNode.Node = new BitcoinNode(storage);
                            BitcoinNode.Node.Start();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(this, ex.Message, MessageBoxType.Error);
                        }
                    }
                });
            }
        }

        public ICommand StopNode
        {
            get
            {
                return new Command((sender, e) =>
                {
                    if (BitcoinNode.Node != null && BitcoinNode.Node.Started)
                    {
                        BitcoinNode.Node.Stop();
                        //todo: who should close storage ?
                        IDisposable storage = BitcoinNode.Node.Storage as IDisposable;
                        storage?.Dispose();
                    }
                });
            }
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