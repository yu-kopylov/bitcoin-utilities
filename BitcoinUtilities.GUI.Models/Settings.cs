using System;
using System.IO;
using System.Runtime.Serialization;
using BitcoinUtilities.GUI.Models.Formats;
using NLog;

namespace BitcoinUtilities.GUI.Models
{
    public class Settings
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public Settings()
        {
            SetDefaults();
        }

        /// <summary>
        /// The folder in which configuration is stored.
        /// </summary>
        public static string SettingsFolder
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
        public static string SettingsFilename
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
        public void SetDefaults()
        {
            string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            BlockchainFolder = Path.Combine(documentsFolder, "BitcoinUtilities", "Blockchain");
            WalletFolder = Path.Combine(documentsFolder, "BitcoinUtilities", "Wallet");
        }

        /// <summary>
        /// Loads settings from the specified folder.
        /// </summary>
        /// <returns>
        /// true if the setting was loaded successfully; otherwise, false.
        /// </returns>
        public bool Load(string folder)
        {
            string fullFilename = Path.Combine(folder, SettingsFilename);
            SettingsFormat settingsFormat;
            try
            {
                settingsFormat = SettingsFormat.Read(fullFilename);
            }
            catch (IOException e)
            {
                logger.Error(e, "Could not read the setting file.");
                return false;
            }
            catch (SerializationException e)
            {
                logger.Error(e, "Invalid settings file format.");
                return false;
            }
            settingsFormat.ApplyTo(this);
            return true;
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