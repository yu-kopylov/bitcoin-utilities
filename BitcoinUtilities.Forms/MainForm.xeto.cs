using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BitcoinUtilities.Storage;
using Eto.Forms;
using Eto.Serialization.Xaml;

namespace BitcoinUtilities.Forms
{
    public class MainForm : Form, INotifyPropertyChanged
    {
        private BitcoinNode node;

        public MainForm()
        {
            XamlReader.Load(this);

            DataContext = this;
        }

        public BitcoinNode Node
        {
            get { return node; }
            set
            {
                node = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICommand StartNode
        {
            get
            {
                return new Command((sender, e) =>
                {
                    if (Node == null || !Node.Started)
                    {
                        try
                        {
                            //todo: use settings for storage location
                            BlockChainStorage storage = BlockChainStorage.Open(@"D:\Temp\Blockchain");
                            Node = new BitcoinNode(storage);
                            Node.Start();
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
                    if (Node != null && Node.Started)
                    {
                        Node.Stop();
                        //todo: who should close storage ?
                        IDisposable storage = Node.Storage as IDisposable;
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