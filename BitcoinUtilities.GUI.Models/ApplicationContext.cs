using BitcoinUtilities.Node;

namespace BitcoinUtilities.GUI.Models
{
    public class ApplicationContext
    {
        private readonly EventManager eventManager = new EventManager();

        public ApplicationContext()
        {
            Settings = new Settings();
            Settings.Load(Settings.SettingsFolder);

            //todo: stop ApplicationContext with EventManager and BitcoinNode ?
            eventManager.Start();
        }

        public Settings Settings { get; set; }

        public BitcoinNode BitcoinNode { get; set; }

        public EventManager EventManager
        {
            get { return eventManager; }
        }
    }
}