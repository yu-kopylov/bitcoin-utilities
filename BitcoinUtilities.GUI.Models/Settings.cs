using System;
using System.IO;
using BitcoinUtilities.GUI.Models.Formats;

namespace BitcoinUtilities.GUI.Models
{
    public class Settings
    {
        public Settings()
        {
            Reset();
        }

        /// <summary>
        /// The folder in which configuration is stored.
        /// </summary>
        public string SettingsFolder
        {
            get
            {
                string localAppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(localAppDataFolder, "BitcoinUtilities", "GUI");
            }
        }

        /// <summary>
        /// The filename of the settings file without a folder.
        /// </summary>
        public string SettingsFilename
        {
            get { return "settings.json"; }
        }

        /// <summary>
        /// A folder in which a blockchain would be stored.
        /// </summary>
        public string BlockchainFolder { get; set; }

        /// <summary>
        /// A folder in which user personal keys would be stored.
        /// </summary>
        public string WalletFolder { get; set; }

        /// <summary>
        /// Sets default settings values.
        /// </summary>
        public void Reset()
        {
            string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            BlockchainFolder = Path.Combine(documentsFolder, "BitcoinUtilities", "Blockchain");
            WalletFolder = Path.Combine(documentsFolder, "BitcoinUtilities", "Wallet");
        }

        /// <summary>
        /// Loads settings from the specified folder.
        /// </summary>
        public void Load(string folder)
        {
            string fullFilename = Path.Combine(folder, SettingsFilename);
            SettingsFormat settingsFormat = SettingsFormat.Read(fullFilename);
            settingsFormat.ApplyTo(this);
        }

        /// <summary>
        /// Writes settings to the specified folder.
        /// </summary>
        public void Save(string folder)
        {
            Directory.CreateDirectory(folder);
            string fullFilename = Path.Combine(folder, SettingsFilename);
            SettingsFormat settingsFormat = SettingsFormat.CreateFrom(this);
            SettingsFormat.Write(fullFilename, settingsFormat);
        }
    }
}