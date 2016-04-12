using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace BitcoinUtilities.GUI.Models.Formats
{
    [DataContract(Name = "Settings")]
    internal class SettingsFormat
    {
        [DataMember]
        public string BlockchainFolder { get; set; }

        [DataMember]
        public string WalletFolder { get; set; }

        public static SettingsFormat CreateFrom(Settings settings)
        {
            SettingsFormat res = new SettingsFormat();

            res.BlockchainFolder = settings.BlockchainFolder;
            res.WalletFolder = settings.WalletFolder;

            return res;
        }

        public void ApplyTo(Settings settings)
        {
            settings.BlockchainFolder = BlockchainFolder;
            settings.WalletFolder = WalletFolder;
        }

        public static SettingsFormat Read(string filename)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(SettingsFormat));
            //todo: handle errors
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return (SettingsFormat) ser.ReadObject(stream);
            }
        }

        public static void Write(string filename, SettingsFormat settings)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(SettingsFormat));
            //todo: handle errors ?
            using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                ser.WriteObject(stream, settings);
                stream.Flush();
            }
        }
    }
}