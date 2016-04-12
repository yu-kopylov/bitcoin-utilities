using BitcoinUtilities.Node;

namespace BitcoinUtilities.GUI.Models
{
    public class ApplicationContext
    {
        public ApplicationContext()
        {
            Settings = new Settings();
            Settings.Load(Settings.SettingsFolder);
        }

        public Settings Settings { get; set; }

        public BitcoinNode BitcoinNode { get; set; }
    }
}