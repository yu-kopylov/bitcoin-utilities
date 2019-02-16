using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using BitcoinUtilities.GUI.Models;

namespace BitcoinUtilities.GUI.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationContext applicationContext;

        private string network;
        private string blockchainFolder;
        private string walletFolder;

        public SettingsViewModel(ApplicationContext applicationContext)
        {
            this.applicationContext = applicationContext;

            Init(applicationContext.Settings);
        }

        public string Network
        {
            get { return network; }
            set
            {
                network = value;
                OnPropertyChanged();
            }
        }

        public string[] KnownNetworks { get; } = NetworkParameters.KnownNetworks.Select(n => n.Name).ToArray();

        public string BlockchainFolder
        {
            get { return blockchainFolder; }
            set
            {
                blockchainFolder = value;
                OnPropertyChanged();
            }
        }

        public string WalletFolder
        {
            get { return walletFolder; }
            set
            {
                walletFolder = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Apply()
        {
            applicationContext.Settings.Network = Network;
            applicationContext.Settings.BlockchainFolder = BlockchainFolder;
            applicationContext.Settings.WalletFolder = WalletFolder;
            //todo: handle error
            applicationContext.Settings.Save(Settings.SettingsFolder);
        }

        public void SetDefaults()
        {
            Settings settings = new Settings();
            Init(settings);
        }

        private void Init(Settings settings)
        {
            Network = settings.Network;
            BlockchainFolder = settings.BlockchainFolder;
            WalletFolder = settings.WalletFolder;
        }
    }
}